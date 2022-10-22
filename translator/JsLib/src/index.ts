import * as monaco from 'monaco-editor';

let editor: monaco.editor.IStandaloneCodeEditor | null = null;

export function loadEditor(): void {
    editor = monaco.editor.create(document.getElementById('editor')!);
    editor.getModel()!.setEOL(monaco.editor.EndOfLineSequence.LF);
}

export function getProgram(): string {
    return editor!.getValue();
}
