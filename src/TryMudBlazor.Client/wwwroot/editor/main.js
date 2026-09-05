require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' } });

let _dotNetInstance;

const throttleLastTimeFuncNameMappings = {};

// Completion snippets are static files; fetch each once and reuse the parsed JSON for every keystroke.
const snippetFileCache = {};

function loadSnippets(file) {
    if (!snippetFileCache[file]) {
        snippetFileCache[file] = fetch(file)
            .then((response) => response.json())
            .catch((error) => {
                // Don't cache a failed fetch, or one network blip disables snippets until reload.
                delete snippetFileCache[file];
                throw error;
            });
    }

    return snippetFileCache[file];
}

function registerLanguageProvider(language) {
    monaco.languages.registerCompletionItemProvider(language, {
        provideCompletionItems: async function (model, position) {
            const textUntilPosition = model.getValueInRange({
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: position.lineNumber,
                endColumn: position.column,
            });

            // Inside an unclosed brace of a .razor file the user is writing C#; otherwise offer component snippets.
            const openBraces = (textUntilPosition.match(/{/g) || []).length;
            const closeBraces = (textUntilPosition.match(/}/g) || []).length;
            const snippetFile = language === 'razor' && openBraces === closeBraces
                ? "editor/snippets/mudblazor.json"
                : "editor/snippets/csharp.json";
            const data = await loadSnippets(snippetFile);

            var word = model.getWordUntilPosition(position);
            var range = {
                startLineNumber: position.lineNumber,
                endLineNumber: position.lineNumber,
                startColumn: word.startColumn,
                endColumn: word.endColumn,
            };
            
            var response = Object.keys(data).map(key => {
                return {
                    label: data[key].prefix,
                    detail : data[key].description,
                    documentation : data[key].body.join('\n'),
                    insertText: data[key].body.join('\n'),
                    kind: monaco.languages.CompletionItemKind.Snippet,
                    insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
                    range: range
                }
            });
            return {
                suggestions: response,
            };
        },
    });
}

function onKeyDown(e) {
    if (e.ctrlKey && e.keyCode === 83) {
        e.preventDefault();

        if (_dotNetInstance && _dotNetInstance.invokeMethodAsync) {
            throttle(() => _dotNetInstance.invokeMethodAsync('TriggerCompileAsync'), 1000, 'compile');
        }
    }
}

function throttle(func, timeFrame, id) {
    const now = new Date();
    if (now - throttleLastTimeFuncNameMappings[id] >= timeFrame) {
        func();

        throttleLastTimeFuncNameMappings[id] = now;
    }
}

// Listen for hot-reload signals when running inside the preview iframe
if (window.frameElement) {
    window.addEventListener('message', function (e) {
        if (e.origin !== window.location.origin) return;
        if (e.data?.type === 'hotReload' && window._hotReloadRef) {
            window._hotReloadRef.invokeMethodAsync('HotReload');
        }
    });
}

window.Try = {
    initialize: function (dotNetInstance) {
        _dotNetInstance = dotNetInstance;
        throttleLastTimeFuncNameMappings['compile'] = new Date();

        Split(['#user-code-editor-container', '#user-page-window-container'], {
            gutterSize: 6,
        })
        window.addEventListener('keydown', onKeyDown);
    },
    changeDisplayUrl: function (url) {
        if (!url) {return; }
        window.history.pushState(null, null, url);
    },
    reloadIframe: function (id, newSrc) {
        const iFrame = document.getElementById(id);
        if (!iFrame) { return; }

        if (!newSrc) {
            iFrame.contentWindow.location.reload();
        } else if (iFrame.src !== `${window.location.origin}${newSrc}`) {
            iFrame.src = newSrc;
        } else {
            // There needs to be some change so the iFrame is actually reloaded
            iFrame.src = '';
            setTimeout(() => iFrame.src = newSrc);
        }
    },
    dispose: function () {
        _dotNetInstance = null;
        window.removeEventListener('keydown', onKeyDown);
    },
    registerHotReload: function (dotNetRef) {
        window._hotReloadRef = dotNetRef;
        window._hotReloadReady = true;
    },
    unregisterHotReload: function () {
        window._hotReloadRef = null;
        window._hotReloadReady = false;
    },
    requestFullReload: function (src) {
        window.parent.Try.reloadIframe('user-page-window', src);
    },
}

window.Try.Editor = window.Try.Editor || (function () {
    let _editor;
    let _overrideValue;

    return {
        create: function (id, value, language) {
            if (!id) { return; }
            let _theme = "default";
            let userPreferences = localStorage.getItem("userPreferences");
            if (userPreferences) {
                const userPreferencesJSON = JSON.parse(userPreferences);
                if (userPreferencesJSON.hasOwnProperty("DarkTheme") && userPreferencesJSON.DarkTheme) {
                    _theme = "vs-dark";
                }
            }

            require(['vs/editor/editor.main'], () => {
                _editor = monaco.editor.create(document.getElementById(id), {
                    value: _overrideValue || value || '',
                    language: language || 'razor',
                    theme: _theme,
                    automaticLayout: true,
                    mouseWheelZoom: true,
                    bracketPairColorization: {
                        enabled: true
                    },
                    minimap: {
                        enabled: false
                    }
                });

                _overrideValue = null;

                monaco.languages.html.razorDefaults.setModeConfiguration({
                    completionItems: true,
                    diagnostics:  true,
                    documentFormattingEdits: true,
                    documentHighlights: true,
                    documentRangeFormattingEdits: true,
                });

                registerLanguageProvider('razor');
                registerLanguageProvider('csharp');
            })
        },
        getValue: function () {
            return _editor.getValue();
        },
        setValue: function (value) {
            if(_editor) {
                _editor.setValue(value);
            } else {
                _overrideValue = value;
            }
        },
        focus: function () {
            return _editor.focus();
        },
        setLanguage: function (language) {
            if(_editor) {
                monaco.editor.setModelLanguage(_editor.getModel(), language);
            }
        },
        setTheme: function (theme) {
            monaco.editor.setTheme(theme);
        },
        dispose: function () {
            _editor = null;
        }
    }
}());

window.Try.CodeExecution = window.Try.CodeExecution || (function () {
    const UNEXPECTED_ERROR_MESSAGE = 'An unexpected error has occurred. Please try again later or contact the team.';
    const USER_COMPONENTS_DLL_STORAGE_KEY = 'TryMudBlazor.UserComponentsDllBase64';

    return {
        hotReloadIframe: function (id, fallbackSrc) {
            const iFrame = document.getElementById(id);
            if (!iFrame) return;

            const iframeWindow = iFrame.contentWindow;
            if (iframeWindow && iframeWindow._hotReloadReady) {
                // Iframe is live — signal it to hot-reload from sessionStorage
                iframeWindow.postMessage({ type: 'hotReload' }, window.location.origin);
            } else {
                // Iframe not yet ready (first run) — fall back to full navigation
                Try.reloadIframe(id, fallbackSrc);
            }
        },
        clearUserComponentsDll: function () {
            window.sessionStorage.removeItem(USER_COMPONENTS_DLL_STORAGE_KEY);
        },
        updateUserComponentsDll: function (dllData) {
            if (!dllData) return;

            // .NET byte[] arrives as a Uint8Array via the runtime's byte-array interop optimization.
            // A plain string means the caller passed a pre-encoded base64 constant (e.g. DefaultUserComponentsAssemblyBytes).
            let dllBase64;
            if (typeof dllData === 'string') {
                dllBase64 = dllData;
            } else {
                // Uint8Array → base64, processed in chunks to avoid call-stack overflow on large arrays.
                let binary = '';
                const chunk = 8192;
                for (let i = 0; i < dllData.length; i += chunk) {
                    binary += String.fromCharCode(...dllData.subarray(i, i + chunk));
                }
                dllBase64 = btoa(binary);
            }

            try {
                window.sessionStorage.setItem(USER_COMPONENTS_DLL_STORAGE_KEY, dllBase64);
            } catch (error) {
                console.error('Failed to store compiled user components DLL', error);
                alert(UNEXPECTED_ERROR_MESSAGE);
            }
        }
    };
}());
