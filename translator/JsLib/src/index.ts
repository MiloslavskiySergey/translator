import * as monaco from 'monaco-editor';

let main: DotNet.DotNetObject | null = null;
let editor: monaco.editor.IStandaloneCodeEditor | null = null;

export function saveMain(mainObj: DotNet.DotNetObject): void {
    main = mainObj;
}

export function updateProgram(program: string): void {
    if (editor!.getValue() != program) {
        editor!.setValue(program);
    }
}

export function loadEditor(): void {
    editor = monaco.editor.create(document.getElementById('editor')!);
    const model = editor.getModel()!;
    model.setEOL(monaco.editor.EndOfLineSequence.LF);
    model.onDidChangeContent(async () => {
        await main!.invokeMethodAsync<void>('UpdateProgram', model.getValue());
    });
}
