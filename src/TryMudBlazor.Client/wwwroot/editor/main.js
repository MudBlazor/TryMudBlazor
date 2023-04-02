require.config({ paths: { 'vs': 'lib/monaco-editor/min/vs' } });

let _editor;
let _overrideValue;
let _currentLanguage;

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

window.Try = {
    initialize: function () {
        Split(['#user-code-editor-container', '#user-page-window-container'])
    },
    changeDisplayUrl: function (url) {
        if (!url) {return; }
        window.history.pushState(null, null, url);
    },
}
window.Try.Editor = {
    create: function (editorId, value, language) {
        if (!editorId) { return; }
        require(['vs/editor/editor.main'], () => {
            _editor = monaco.editor.create(document.getElementById(editorId), {
                value: value || '',
                language: language || 'razor',
                automaticLayout: true,
            });

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
}

window.Try.CodeExecution = window.Try.CodeExecution || (function () {
    const UNEXPECTED_ERROR_MESSAGE = 'An unexpected error has occurred. Please try again later or contact the team.';

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

    return {
        updateUserComponentsDll: async function (fileContent) {
            if (!fileContent) {
                return;
            }

            const cache = await caches.open('blazor-resources-/');

            const cacheKeys = await cache.keys();
            const userComponentsDllCacheKey = cacheKeys.find(x => x.url.indexOf('Try.UserComponents.dll') > -1);
            if (!userComponentsDllCacheKey || !userComponentsDllCacheKey.url) {
                alert(UNEXPECTED_ERROR_MESSAGE);
                return;
            }

            const dllPath = userComponentsDllCacheKey.url.substr(window.location.origin.length);
            fileContent = typeof fileContent === 'number' ? BINDING.conv_string(fileContent) : fileContent // tranfering raw pointer to the memory of the mono string
            const dllBytes = typeof fileContent === 'string' ? convertBase64StringToBytes(fileContent) : fileContent;

            await putInCacheStorage(cache, dllPath, dllBytes);
        }
    };
}());
