require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' } });

let _dotNetInstance;

const throttleLastTimeFuncNameMappings = {};
const CACHE_NAME = 'dotnet-resources-/';

function registerLangugageProvider(language) {
    monaco.languages.registerCompletionItemProvider(language, {
        provideCompletionItems: async function (model, position) {
            var textUntilPosition = model.getValueInRange({
                startLineNumber: 1,
                startColumn: 1,
                endLineNumber: position.lineNumber,
                endColumn: position.column,
            });

            if(language == 'razor')
            {
                if ((textUntilPosition.match(/{/g) || []).length !== (textUntilPosition.match(/}/g) || []).length) {
                    var data = await fetch("editor/snippets/csharp.json").then((response) => response.json());
                } else {
                    var data = await fetch("editor/snippets/mudblazor.json").then((response) => response.json());
                }
            }else {
                var data = await fetch("editor/snippets/csharp.json").then((response) => response.json());
            }
            
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
    clearCache: async function () {
        const cacheName = CACHE_NAME;
        try {
            const cache = await caches.open(cacheName);
            const keys = await cache.keys();

            await Promise.all(keys.map(key => {
                return cache.delete(key);
            }));
            console.log(`Cache '${cacheName}' has been cleared.`);
        } catch (error) {
            console.error('Error clearing cache:', error);
        }
    },
    dispose: function () {
        _dotNetInstance = null;
        window.removeEventListener('keydown', onKeyDown);
    }
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

                registerLangugageProvider('razor');
                registerLangugageProvider('csharp');
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

    function putInCacheStorage(cache, fileName, fileBytes, contentType) {
        const cachedResponse = new Response(
            new Blob([fileBytes]),
            {
                headers: {
                    'Content-Type': contentType || 'application/octet-stream',
                    'Content-Length': fileBytes.length.toString()
                }
            });

        return cache.put(fileName, cachedResponse);
    }

    function convertBase64StringToBytes(base64String) {
        const binaryString = window.atob(base64String);

        const bytesCount = binaryString.length;
        const bytes = new Uint8Array(bytesCount);
        for (let i = 0; i < bytesCount; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }

        return bytes;
    }

    function convertBytesToBase64String(bytes) {
        let binaryString = '';
        for (let i = 0; i < bytes.length; i++) {
            binaryString += String.fromCharCode(bytes[i]);
        }

        return window.btoa(binaryString);
    }

    return {
        getCompilationDlls: async function (dllNames) {
            const config = window.Blazor && Blazor.runtime && Blazor.runtime.getConfig ? Blazor.runtime.getConfig() : null;
            const resources = config && config.resources ? config.resources : {};

            const getAssemblyAssetName = (simpleName) => {
                const virtualName = `${simpleName}.dll`;
                const arrayGroups = [resources.coreAssembly, resources.assembly].filter(Array.isArray);
                for (const group of arrayGroups) {
                    const asset = group.find(x => x && (x.virtualPath === virtualName || x.name === virtualName));
                    if (asset && asset.name) {
                        return asset.name;
                    }
                }
                throw new Error(`.NET 10 boot config assembly asset not found for '${virtualName}'`);
            };

            const fetchDllBytes = async (dllName) => {
                const assetName = getAssemblyAssetName(dllName);
                const urls = [
                    `_framework/${assetName}`,
                    `_framework/${dllName}.dll`
                ];

                for (const url of urls) {
                    const response = await fetch(url, { cache: 'force-cache' });
                    if (response && response.ok) {
                        return new Uint8Array(await response.arrayBuffer());
                    }
                }

                throw new Error(`Failed to fetch compilation DLL '${dllName}' (resolved '${assetName}')`);
            };

            return Promise.all(dllNames.map(dll => fetchDllBytes(dll)));
        },

        updateUserComponentsDll: async function (fileContent) {
            if (!fileContent) {
                return;
            }
            fileContent = typeof fileContent === 'number' ? BINDING.conv_string(fileContent) : fileContent // tranfering raw pointer to the memory of the mono string
            const dllBytes = typeof fileContent === 'string' ? convertBase64StringToBytes(fileContent) : fileContent;
            const dllBase64 = convertBytesToBase64String(dllBytes);

            try {
                window.sessionStorage.setItem(USER_COMPONENTS_DLL_STORAGE_KEY, dllBase64);
            } catch (error) {
                console.error('Failed to store compiled user components DLL', error);
                alert(UNEXPECTED_ERROR_MESSAGE);
            }
        }
    };
}());
