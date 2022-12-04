namespace translator.Services;

public interface ITerminalService
{
    void Open();
    void Close();
    void Write(string value);
    string? Read();
}
