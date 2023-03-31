
window.try = window.try || {};

window.try.editor = {

    create: function(editorId, value, language){
        if (!editorId) {
            return;
        }

        require.config({ paths: { 'vs': '../../lib/monaco-editor/min/vs' } });

        monaco.editor.create(document.getElementById(editorId), {
            value: _overrideValue || value || '',
            language: language || _currentLanguage || 'razor'
        });
    }

}