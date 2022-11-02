namespace translator.Utils;

public class TreeViewItem
{
    public string Text { get; set; }
    public IEnumerable<TreeViewItem> Children { get; set; }

    public TreeViewItem(string text, IEnumerable<TreeViewItem> children)
    {
        Text = text;
        Children = children;
    }

    public TreeViewItem(string text)
    {
        Text = text;
        Children = new List<TreeViewItem>();
    }
}
