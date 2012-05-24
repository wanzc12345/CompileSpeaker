using System;
using SpeechLib;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FarsiLibrary.Win;
using FastColoredTextBoxNS;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Drawing.Drawing2D;
using System.Diagnostics;

namespace CompileSpeaker
{
    public partial class PowerfulCSharpEditor : Form
    {
        string[] keywords = { "abstract", "super", "native", "boolean", "break", "byte", "case", "catch", "char", "checked", "class", 
                                "const", "continue", "int", "default", "package", "do", "double", "else", "enum", "event",
                                "explicit", "synchronized", "false", "finally", "instanceof", "float", "for", "foreach", "goto", "if",
                                "implicit", "int", "interface", "import", "implements", "long", "extends", "new", "final",
                                "null", "object", "operator", "out", "override", "strictfp", "private", "protected", "public",
                                "volatile", "transient", "return", "throws", "short", "sizeof", "static", "String", 
                                "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
                                "ushort", "using", "virtual", "void", "volatile", "while"};
        string[] methods = { "finalize()", "getClass()", "equals()", "clone()", "hashCode()", "notify()", "notifyAll()", "toString()",
                           "wait()","wait(^)"};
        string[] snippets = { "if(^)\n{\n;\n}", "if(^)\n{\n;\n}\nelse\n{\n;\n}", "for(^;;)${\n;\n}", "while(^)\n{\n;\n}", "do${\n^;\n}while();", "switch(^)\n{\ncase : break;\n}" };
        string[] declarationSnippets = { 
               "public class ^\n{\n}", "private class ^\n{\n}", "protected class ^\n{\n}",
               "public struct ^\n{\n;\n}", "private struct ^\n{\n;\n}", "internal struct ^\n{\n;\n}",
               "public void ^()\n{\n;\n}", "private void ^()\n{\n;\n}", "internal void ^()\n{\n;\n}", "protected void ^()\n{\n;\n}",
               "public ^{  }", "private ^{}", "internal ^{ }", "protected ^{  }","try{\n^\n}catch(Exception e){\n\n}","new ^(){\n}",
               "public static void main(String[] argv)\n{\n\t\n^}"
               };
        Style invisibleCharsStyle = new InvisibleCharsRenderer(Pens.Gray);

        public PowerfulCSharpEditor()
        {
            InitializeComponent();

            //init menu images
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PowerfulCSharpEditor));
            copyToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("copyToolStripButton.Image")));
            cutToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("cutToolStripButton.Image")));
            pasteToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("pasteToolStripButton.Image")));
        }


        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateTab(null);
        }

        private void CreateTab(string fileName)
        {
            try
            {
                var tb = new FastColoredTextBox();
                tb.ContextMenuStrip = cmMain;
                tb.Dock = DockStyle.Fill;
                tb.BorderStyle = BorderStyle.Fixed3D;
                tb.Language = Language.CSharp;
                tb.AddStyle(new CurrentLineHighlighter(Color.FromArgb(255, 250, 250, 255), Color.FromArgb(255, 220, 220, 255)));//curent line highlighter
                tb.AddStyle(new MarkerStyle(new SolidBrush(Color.FromArgb(40, Color.Gray))));//same words style
                var tab = new FATabStripItem(fileName!=null?Path.GetFileName(fileName):"[new]", tb);
                tab.Tag = fileName;
                if (fileName != null)
                    tb.Text = File.ReadAllText(fileName);
                tb.ClearUndo();
                tb.IsChanged = false;
                tsFiles.AddTab(tab);
                tsFiles.SelectedItem = tab;
                tb.Focus();
                tb.SelectionStart = 0;
                tb.DoCaretVisible();
                tb.DelayedTextChangedInterval = 1000;
                tb.TextChangedDelayed += new EventHandler<TextChangedEventArgs>(tb_TextChangedDelayed);
                tb.SelectionChangedDelayed += new EventHandler(tb_SelectionChangedDelayed);
                tb.KeyDown += new KeyEventHandler(tb_KeyDown);
                tb.MouseMove += new MouseEventHandler(tb_MouseMove);
                tb.SelectionChanged += new EventHandler(tb_SelectionChanged);
                tb.VisibleRangeChanged += new EventHandler(tb_VisibleRangeChanged);
                //create autocomplete popup menu
                AutocompleteMenu popupMenu = new AutocompleteMenu(tb);
                popupMenu.Items.ImageList = ilAutocomplete;
                BuildAutocompleteMenu(popupMenu);
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == System.Windows.Forms.DialogResult.Retry)
                    CreateTab(fileName);
            }
        }

        void tb_VisibleRangeChanged(object sender, EventArgs e)
        {
            HighlightCurrentLine(sender as FastColoredTextBox);
        }

        void tb_SelectionChanged(object sender, EventArgs e)
        {
            HighlightCurrentLine(sender as FastColoredTextBox);
        }

        private void HighlightCurrentLine(FastColoredTextBox tb)
        {
            //highlight current line
            tb.VisibleRange.ClearStyle(tb.Styles[0]);
            if (!btHighlightCurrentLine.Checked)
                return;
            var range = new Range(tb, tb.Selection.Start, tb.Selection.Start);
            range.Expand();
            range.SetStyle(tb.Styles[0]);
        }

        private void BuildAutocompleteMenu(AutocompleteMenu popupMenu)
        {
            List<AutocompleteItem> items = new List<AutocompleteItem>();

            foreach (var item in snippets)
                items.Add(new SnippetAutocompleteItem(item) { ImageIndex = 1 });
            foreach (var item in declarationSnippets)
                items.Add(new DeclarationSnippet(item) { ImageIndex = 0 });
            foreach (var item in methods)
                items.Add(new MethodAutocompleteItem(item) { ImageIndex = 2 });
            foreach (var item in keywords)
                items.Add(new AutocompleteItem(item));

            items.Add(new InsertSpaceSnippet());
            items.Add(new InsertSpaceSnippet(@"^(\w+)([=<>!:]+)(\w+)$"));
            items.Add(new InsertEnterSnippet());

            //set as autocomplete source
            popupMenu.Items.SetAutocompleteItems(items);
            popupMenu.SearchPattern = @"[\w\.:=!<>]";
        }

        void tb_MouseMove(object sender, MouseEventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            var place = tb.PointToPlace(e.Location);
            var r = new Range(tb, place, place);
            string text = r.GetFragment("[a-zA-Z]").Text;
            lbWordUnderMouse.Text = text;
        }

        void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.OemMinus)
            {
                NavigateBackward();
                e.Handled = true;
            }

            if (e.Modifiers == (Keys.Control|Keys.Shift) && e.KeyCode == Keys.OemMinus)
            {
                NavigateForward();
                e.Handled = true;
            }
        }

        void tb_SelectionChangedDelayed(object sender, EventArgs e)
        {
            var tb = sender as FastColoredTextBox;
            //remember last visit time
            if (tb.Selection.Start == tb.Selection.End && tb.Selection.Start.iLine < tb.LinesCount)
            {
                if (lastNavigatedDateTime != tb[tb.Selection.Start.iLine].LastVisit)
                {
                    tb[tb.Selection.Start.iLine].LastVisit = DateTime.Now;
                    lastNavigatedDateTime = tb[tb.Selection.Start.iLine].LastVisit;
                }
            }

            //highlight same words
            tb.VisibleRange.ClearStyle(tb.Styles[1]);
            if (tb.Selection.Start != tb.Selection.End)
                return;//user selected diapason
            //get fragment around caret
            var fragment = tb.Selection.GetFragment(@"\w");
            string text = fragment.Text;
            if (text.Length == 0)
                return;
            //highlight same words
            var ranges = tb.VisibleRange.GetRanges("\\b" + text + "\\b").ToArray();
            if (ranges.Length > 1)
                foreach (var r in ranges)
                    r.SetStyle(tb.Styles[1]);
        }

        void tb_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            FastColoredTextBox tb = (sender as FastColoredTextBox);
            //rebuild object explorer
            string text = (sender as FastColoredTextBox).Text;
            ThreadPool.QueueUserWorkItem(
                (o)=>ReBuildObjectExplorer(text)
            );

            //show invisible chars
            HighlightInvisibleChars(e.ChangedRange);
        }

        private void HighlightInvisibleChars(Range range)
        {
            range.ClearStyle(invisibleCharsStyle);
            if (btInvisibleChars.Checked)
                range.SetStyle(invisibleCharsStyle, @".$|.\r\n|\s");
        }

        List<ExplorerItem> explorerList = new List<ExplorerItem>();

        private void ReBuildObjectExplorer(string text)
        {
            try
            {
                List<ExplorerItem> list = new List<ExplorerItem>();
                int lastClassIndex = -1;
                //find classes, methods and properties
                Regex regex = new Regex(@"^(?<range>[\w\s]+\b(class|struct|enum|interface)\s+[\w<>,\s]+)|^\s*(public|private|internal|protected)[^\n]+(\n?\s*{|;)?", RegexOptions.Multiline);
                foreach (Match r in regex.Matches(text))
                    try
                    {
                        string s = r.Value;
                        int i = s.IndexOfAny(new char[] { '=', '{', ';' });
                        if (i >= 0)
                            s = s.Substring(0, i);
                        s = s.Trim();

                        var item = new ExplorerItem() { title = s, position = r.Index };
                        if (Regex.IsMatch(item.title, @"\b(class|struct|enum|interface)\b"))
                        {
                            item.title = item.title.Substring(item.title.LastIndexOf(' ')).Trim();
                            item.type = ExplorerItemType.Class;
                            list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());
                            lastClassIndex = list.Count;
                        }
                        else
                            if (item.title.Contains(" event "))
                            {
                                int ii = item.title.LastIndexOf(' ');
                                item.title = item.title.Substring(ii).Trim();
                                item.type = ExplorerItemType.Event;
                            }
                            else
                                if (item.title.Contains("("))
                                {
                                    var parts = item.title.Split('(');
                                    item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "(" + parts[1];
                                    item.type = ExplorerItemType.Method;
                                }
                                else
                                    if (item.title.EndsWith("]"))
                                    {
                                        var parts = item.title.Split('[');
                                        if (parts.Length < 2) continue;
                                        item.title = parts[0].Substring(parts[0].LastIndexOf(' ')).Trim() + "[" + parts[1];
                                        item.type = ExplorerItemType.Method;
                                    }
                                    else
                                    {
                                        int ii = item.title.LastIndexOf(' ');
                                        item.title = item.title.Substring(ii).Trim();
                                        item.type = ExplorerItemType.Property;
                                    }
                        list.Add(item);
                    }
                    catch { ;}

                list.Sort(lastClassIndex + 1, list.Count - (lastClassIndex + 1), new ExplorerItemComparer());

                BeginInvoke(
                    new Action(() =>
                        {
                            explorerList = list;
                            dgvObjectExplorer.RowCount = explorerList.Count;
                            dgvObjectExplorer.Invalidate();
                        })
                );
            }
            catch { ;}
        }

        enum ExplorerItemType
        {
            Class, Method, Property, Event
        }

        class ExplorerItem
        {
            public ExplorerItemType type;
            public string title;
            public int position;
        }

        class ExplorerItemComparer : IComparer<ExplorerItem>
        {
            public int Compare(ExplorerItem x, ExplorerItem y)
            {
                return x.title.CompareTo(y.title);
            }
        }

        private void tsFiles_TabStripItemClosing(TabStripItemClosingEventArgs e)
        {
            if ((e.Item.Controls[0] as FastColoredTextBox).IsChanged)
            {
                switch(MessageBox.Show("Do you want save " + e.Item.Title + " ?", "Save", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information))
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        if (!Save(e.Item))
                            e.Cancel = true;
                        break;
                    case DialogResult.Cancel:
                         e.Cancel = true;
                        break;
                }
            }
        }

        private bool Save(FATabStripItem tab)
        {
            var tb = (tab.Controls[0] as FastColoredTextBox);
            if (tab.Tag == null)
            {
                if (sfdMain.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    return false;
                tab.Title = Path.GetFileName(sfdMain.FileName);
                tab.Tag = sfdMain.FileName;
            }

            try
            {
                File.WriteAllText(tab.Tag as string, tb.Text);
                tb.IsChanged = false;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(ex.Message, "Error", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error) == DialogResult.Retry)
                    return Save(tab);
                else
                    return false;
            }

            return true;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
                Save(tsFiles.SelectedItem);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (tsFiles.SelectedItem != null)
            {
                string oldFile = tsFiles.SelectedItem.Tag as string;
                tsFiles.SelectedItem.Tag = null;
                if (!Save(tsFiles.SelectedItem))
                if(oldFile!=null)
                {
                    tsFiles.SelectedItem.Tag = oldFile;
                    tsFiles.SelectedItem.Title = Path.GetFileName(oldFile);
                }
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ofdMain.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                CreateTab(ofdMain.FileName);
        }

        FastColoredTextBox CurrentTB
        {
            get {
                if (tsFiles.SelectedItem == null)
                    return null;
                return (tsFiles.SelectedItem.Controls[0] as FastColoredTextBox);
            }
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.Selection.SelectAll();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.UndoEnabled)
                CurrentTB.Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (CurrentTB.RedoEnabled)
                CurrentTB.Redo();
        }

        private void tmUpdateInterface_Tick(object sender, EventArgs e)
        {
            try
            {
                if(CurrentTB != null && tsFiles.Items.Count>0)
                {
                    var tb = CurrentTB;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = tb.UndoEnabled;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = tb.RedoEnabled;
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = tb.IsChanged;
                    saveAsToolStripMenuItem.Enabled = true;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = true;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                    copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = tb.Selection.Start != tb.Selection.End;
                    printToolStripButton.Enabled = true;
                }
                else
                {
                    saveToolStripButton.Enabled = saveToolStripMenuItem.Enabled = false;
                    saveAsToolStripMenuItem.Enabled = false;
                    cutToolStripButton.Enabled = cutToolStripMenuItem.Enabled =
                    copyToolStripButton.Enabled = copyToolStripMenuItem.Enabled = false;
                    pasteToolStripButton.Enabled = pasteToolStripMenuItem.Enabled = false;
                    printToolStripButton.Enabled = false;
                    undoStripButton.Enabled = undoToolStripMenuItem.Enabled = false;
                    redoStripButton.Enabled = redoToolStripMenuItem.Enabled = false;
                    dgvObjectExplorer.RowCount = 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void printToolStripButton_Click(object sender, EventArgs e)
        {
            if(CurrentTB!=null)
            {
                CurrentTB.Print(new PrintDialogSettings());
            }
        }

        bool tbFindChanged = false;

        private void tbFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' && CurrentTB != null)
            {
                Range r = tbFindChanged?CurrentTB.Range.Clone():CurrentTB.Selection.Clone();
                tbFindChanged = false;
                r.End = new Place(CurrentTB[CurrentTB.LinesCount - 1].Count, CurrentTB.LinesCount - 1);
                var pattern = Regex.Replace(tbFind.Text, FastColoredTextBoxNS.FindForm.RegexSpecSymbolsPattern, "\\$0");
                foreach (var found in r.GetRanges(pattern))
                {
                    found.Inverse();
                    CurrentTB.Selection = found;
                    CurrentTB.DoSelectionVisible();
                    return;
                }
                MessageBox.Show("Not found.");
            }
            else
                tbFindChanged = true;
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.ShowFindDialog();
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.ShowReplaceDialog();
        }

        private void PowerfulCSharpEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            List<FATabStripItem> list = new List<FATabStripItem>();
            foreach (FATabStripItem tab in  tsFiles.Items)
                list.Add(tab);
            foreach (var tab in list)
            {
                TabStripItemClosingEventArgs args = new TabStripItemClosingEventArgs(tab);
                tsFiles_TabStripItemClosing(args);
                if (args.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
                tsFiles.RemoveTab(tab);
            }
        }

        private void dgvObjectExplorer_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (CurrentTB != null)
            {
                var item = explorerList[e.RowIndex];
                CurrentTB.GoEnd();
                CurrentTB.SelectionStart = item.position;
                CurrentTB.DoSelectionVisible();
                CurrentTB.Focus();
            }
        }

        private void dgvObjectExplorer_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                ExplorerItem item = explorerList[e.RowIndex];
                if (e.ColumnIndex == 1)
                    e.Value = item.title;
                else
                    switch (item.type)
                    {
                        case ExplorerItemType.Class:
                            e.Value = global::CompileSpeaker.Properties.Resources.class_libraries;
                            return;
                        case ExplorerItemType.Method:
                            e.Value = global::CompileSpeaker.Properties.Resources.box;
                            return;
                        case ExplorerItemType.Event:
                            e.Value = global::CompileSpeaker.Properties.Resources.lightning;
                            return;
                        case ExplorerItemType.Property:
                            e.Value = global::CompileSpeaker.Properties.Resources.property;
                            return;
                    }
            }
            catch{;}
        }

        private void tsFiles_TabStripItemSelectionChanged(TabStripItemChangedEventArgs e)
        {
            if (CurrentTB != null)
            {
                CurrentTB.Focus();
                string text = CurrentTB.Text;
                ThreadPool.QueueUserWorkItem(
                    (o) => ReBuildObjectExplorer(text)
                );
            }
        }

        private void backStripButton_Click(object sender, EventArgs e)
        {
            NavigateBackward();
        }

        private void forwardStripButton_Click(object sender, EventArgs e)
        {
            NavigateForward();
        }

        DateTime lastNavigatedDateTime = DateTime.Now;

        private bool NavigateBackward()
        {
            DateTime max = new DateTime();
            int iLine = -1;
            FastColoredTextBox tb = null;
            for (int iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (int i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit < lastNavigatedDateTime && t[i].LastVisit > max)
                    {
                        max = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                lastNavigatedDateTime = tb[iLine].LastVisit;
                tb.Focus();
                return true;
            }
            else
                return false;
        }

        private bool NavigateForward()
        {
            DateTime min = DateTime.Now;
            int iLine = -1;
            FastColoredTextBox tb = null;
            for (int iTab = 0; iTab < tsFiles.Items.Count; iTab++)
            {
                var t = (tsFiles.Items[iTab].Controls[0] as FastColoredTextBox);
                for (int i = 0; i < t.LinesCount; i++)
                    if (t[i].LastVisit > lastNavigatedDateTime && t[i].LastVisit < min)
                    {
                        min = t[i].LastVisit;
                        iLine = i;
                        tb = t;
                    }
            }
            if (iLine >= 0)
            {
                tsFiles.SelectedItem = (tb.Parent as FATabStripItem);
                tb.Navigate(iLine);
                lastNavigatedDateTime = tb[iLine].LastVisit;
                tb.Focus();
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// This item appears when any part of snippet text is typed
        /// </summary>
        class DeclarationSnippet : SnippetAutocompleteItem
        {
            public DeclarationSnippet(string snippet)
                : base(snippet)
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var pattern = Regex.Replace(fragmentText, FastColoredTextBoxNS.FindForm.RegexSpecSymbolsPattern, "\\$0");
                if (Regex.IsMatch(Text, "\\b" + pattern, RegexOptions.IgnoreCase))
                    return CompareResult.Visible;
                return CompareResult.Hidden;
            }
        }

        /// <summary>
        /// Divides numbers and words: "123AND456" -> "123 AND 456"
        /// Or "i=2" -> "i = 2"
        /// </summary>
        class InsertSpaceSnippet : AutocompleteItem
        {
            string pattern;

            public InsertSpaceSnippet(string pattern)
                : base("")
            {
                this.pattern = pattern;
            }

            public InsertSpaceSnippet()
                : this(@"^(\d*)([a-zA-Z_]+)(\d*)$")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                if (Regex.IsMatch(fragmentText, pattern))
                {
                    Text = InsertSpaces(fragmentText);
                    if (Text != fragmentText)
                        return CompareResult.Visible;
                }
                return CompareResult.Hidden;
            }

            public string InsertSpaces(string fragment)
            {
                var m = Regex.Match(fragment, pattern);
                if (m == null)
                    return fragment;
                if (m.Groups[1].Value == "" && m.Groups[3].Value == "")
                    return fragment;
                return (m.Groups[1].Value + " " + m.Groups[2].Value + " " + m.Groups[3].Value).Trim();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return Text;
                }
            }
        }

        /// <summary>
        /// Inerts line break after '}'
        /// </summary>
        class InsertEnterSnippet : AutocompleteItem
        {
            Place enterPlace = Place.Empty;

            public InsertEnterSnippet()
                : base("[Line break]")
            {
            }

            public override CompareResult Compare(string fragmentText)
            {
                var r = Parent.Fragment.Clone();
                while (r.Start.iChar > 0)
                {
                    if (r.CharBeforeStart == '}')
                    {
                        enterPlace = r.Start;
                        return CompareResult.Visible;
                    }

                    r.GoLeftThroughFolded();
                }

                return CompareResult.Hidden;
            }

            public override string GetTextForReplace()
            {
                //extend range
                Range r = Parent.Fragment;
                Place end = r.End;
                r.Start = enterPlace;
                r.End = r.End;
                //insert line break
                return Environment.NewLine + r.Text;
            }

            public override void OnSelected(AutocompleteMenu popupMenu, SelectedEventArgs e)
            {
                base.OnSelected(popupMenu, e);
                if (Parent.Fragment.tb.AutoIndent)
                    Parent.Fragment.tb.DoAutoIndent();
            }

            public override string ToolTipTitle
            {
                get
                {
                    return "Insert line break after '}'";
                }
            }
        }

        private void autoIndentSelectedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.DoAutoIndent();
        }

        private void btInvisibleChars_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                HighlightInvisibleChars((tab.Controls[0] as FastColoredTextBox).Range);
            if (CurrentTB!=null)
                CurrentTB.Invalidate();
        }

        private void btHighlightCurrentLine_Click(object sender, EventArgs e)
        {
            foreach (FATabStripItem tab in tsFiles.Items)
                HighlightCurrentLine(tab.Controls[0] as FastColoredTextBox);
            if (CurrentTB != null)
                CurrentTB.Invalidate();
        }

        private void commentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.InsertLinePrefix("//");
        }

        private void uncommentSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CurrentTB.RemoveLinePrefix("//");
        }

        private void cloneLinesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
        }

        private void cloneLinesAndCommentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //start autoUndo block
            CurrentTB.BeginAutoUndo();
            //expand selection
            CurrentTB.Selection.Expand();
            //get text of selected lines
            string text = Environment.NewLine + CurrentTB.Selection.Text;
            //comment lines
            CurrentTB.InsertLinePrefix("//");
            //move caret to end of selected lines
            CurrentTB.Selection.Start = CurrentTB.Selection.End;
            //insert text
            CurrentTB.InsertText(text);
            //end of autoUndo block
            CurrentTB.EndAutoUndo();
        }

        private void compileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //TODO: to be continued
            string cmd = "javac hello.java";
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = "/C" + cmd;
            if (process.Start())
            {
                process.WaitForExit(5000);
            }
            string strRst = process.StandardOutput.ReadToEnd();
            process.Close();
            System.Console.Write("Result:"+strRst);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            //isrg.DictationSetState(SpeechRuleState.SGDSInactive);
            //SpeechLib.SpSharedRecoContextClass ssrc = new SpSharedRecoContextClass();
            //ssrc.Recognition +=new _ISpeechRecoContextEvents_RecognitionEventHandler(ssrc_Recognition);
            //SpeechLib.ISpeechRecoGrammar isrg = ssrc.CreateGrammar(1);
            //isrg.DictationSetState(SpeechRuleState.SGDSActive);
        }

        void ssrc_EndStream(int i, object o,bool b)
        {
            MessageBox.Show("Thank you!");
        }

        public void ssrc_Recognition(int i, object o, SpeechLib.SpeechRecognitionType srt, SpeechLib.ISpeechRecoResult isrr) {
            MessageBox.Show(isrr.PhraseInfo.GetText(0, -1, true));
            executeCommand(isrr.PhraseInfo.GetText(0, -1, true));
        }

        public void executeCommand(string command) {
            //do something here
            if (command.Equals("hello"))
            {
                SpeechLib.SpVoiceClass SVC = new SpeechLib.SpVoiceClass();
                SpeechLib.ISpeechObjectTokens voice = SVC.GetVoices("Language = 804 ", "");
                SVC.Voice = voice.Item(0);
                SVC.Speak("hello!");
            }
            else if (command.StartsWith("open")) {
                open_Speaker();
            }
            else if (command.Equals("save"))
            {
                save_Speaker();
            }
            else if (command.StartsWith("close"))
            {
                close_Speaker();
            }
            else if (command.Equals("compile"))
            {
                Compile_Speaker();
            }
            else if (command.Equals("run"))
            {

            }
            else if (command.StartsWith("up"))
            {
                MoveUp_Speaker();
            }
            else if (command.StartsWith("down"))
            {
                MoveDown_Speaker();
            }
            else if (command.StartsWith("left"))
            {
                MoveLeft_Speaker();
            }
            else if (command.StartsWith("right"))
            {
                MoveRight_Speaker();
            }
            else if (command.StartsWith("go"))
            {

            }
            else if (command.Equals("start"))
            {

            }
            else if (command.Equals("end"))
            {

            }
            else if (command.Equals("copy"))
            {
                copy_Speaker();
            }
            else if (command.Equals("cut"))
            {
                cut_Speaker();
            }
            else if (command.Equals("paste"))
            {
                paste_Speaker();
            }
            else if (command.Equals("delete"))
            {

            }
            else
            {
                SpeechLib.SpVoiceClass SVC = new SpeechLib.SpVoiceClass();
                SpeechLib.ISpeechObjectTokens voice = SVC.GetVoices("Language = 804 ", "");
                SVC.Voice = voice.Item(0);
                SVC.Speak("对不起，我没听懂!");
            }
        }

        //API START

        private void open_Speaker() {
            if (ofdMain.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                CreateTab(ofdMain.FileName);
        }

        private void save_Speaker() {
            if (tsFiles.SelectedItem != null)
                Save(tsFiles.SelectedItem);
        }

        private void close_Speaker() {
            tsFiles.RemoveTab(tsFiles.Items[0]);
        }

        private void cut_Speaker()
        {
            CurrentTB.Cut();
        }

        private void copy_Speaker()
        {
            CurrentTB.Copy();
        }

        private void paste_Speaker()
        {
            CurrentTB.Paste();
        }

        private void selectAll_Speaker()
        {
            CurrentTB.Selection.SelectAll();
        }

        private void undo_Speaker()
        {
            if (CurrentTB.UndoEnabled)
                CurrentTB.Undo();
        }

        private void redo_Speaker()
        {
            if (CurrentTB.RedoEnabled)
                CurrentTB.Redo();
        }

        private void MoveLeft_Speaker()
        {
            FastColoredTextBox tb = (tsFiles.Items[0].Controls[0] as FastColoredTextBox);

            if (tb.SelectionStart == 0)
                return;

            tb.SelectionStart = tb.SelectionStart - 1;       //光标所在位置索引
            tb.Focus();
        }

        private void MoveRight_Speaker()
        {
            FastColoredTextBox tb = (tsFiles.Items[0].Controls[0] as FastColoredTextBox);
            string strCmdText = tb.Text;

            if (strCmdText.Length == tb.SelectionStart)
                return;

            if (strCmdText[tb.SelectionStart] == '\r')
                tb.SelectionStart = tb.SelectionStart + 2;
            else
                tb.SelectionStart = tb.SelectionStart + 1;       //光标所在位置索引

            tb.Focus();
        }

        private void MoveUp_Speaker()
        {
            FastColoredTextBox tb = (tsFiles.Items[0].Controls[0] as FastColoredTextBox);
            string strCmdText = tb.Text;
            int curInx = tb.SelectionStart;       //光标所在位置索引
            string temp = strCmdText.Substring(0, curInx);            //从头到光标的字符串
            int curLineStart = temp.LastIndexOf('\n') + 1;            //当前行的开始位置
            int curLineLeftLenth = curInx - curLineStart;             //当前行开始到光标的长度
            int p = curLineStart - 2;
            if (p >= 0)
            {
                while (temp[p - 1] != '\n')
                {
                    p--;
                    if (p == 0)
                        break;
                }
                int upLineLength = curLineStart - p;
                if ((upLineLength - 2) >= curLineLeftLenth)
                    tb.SelectionStart = tb.SelectionStart - upLineLength;
                else
                    tb.SelectionStart = curLineStart - 2;
                tb.Focus();
            }
        }

        private void MoveDown_Speaker()
        {
            FastColoredTextBox tb = (tsFiles.Items[0].Controls[0] as FastColoredTextBox);
            string strCmdText = tb.Text;
            int curInx = tb.SelectionStart;       //光标所在位置索引
            string temp = strCmdText.Substring(0, curInx);            //从头到光标的字符串
            int curLineStart = temp.LastIndexOf('\n') + 1;            //当前行的开始位置
            int curLineLeftLenth = curInx - curLineStart;             //当前行开始到光标的长度
            int p = curInx;
            if (strCmdText.Length == (p))
                return;
            while (strCmdText[p] != '\n')
            {
                p++;
                if (strCmdText.Length == (p))
                    return;
            }
            p++;
            int curLineLength = p - curInx + curLineLeftLenth;

            temp = strCmdText.Substring(p);
            if (temp.IndexOf('\n') == -1)
            {
                if ((curInx + curLineLength) > strCmdText.Length)
                    tb.SelectionStart = strCmdText.Length;
                else
                    tb.SelectionStart = curInx + curLineLength;
            }
            else
            {
                if ((curInx + curLineLength) > (temp.IndexOf('\n') + p))
                    tb.SelectionStart = temp.IndexOf('\n') - 1 + p;
                else
                    tb.SelectionStart = curInx + curLineLength;
            }
            tb.Focus();
        }

        private void Compile_Speaker() {
            //TODO: to be continued
            string cmd = "javac hello.java";
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = "/C" + cmd;
            if (process.Start())
            {
                process.WaitForExit(5000);
            }
            string strRst = process.StandardOutput.ReadToEnd();
            process.Close();
            System.Console.Write("Result:" + strRst);
        }

        private void Run_Speaker() { 
        }

        //API END

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

    }

    public class CurrentLineHighlighter : MarkerStyle
    {
        Color color1;
        Color color2;

        public CurrentLineHighlighter(Color color1, Color color2)
            : base(null)
        {
            this.color1 = color1;
            this.color2 = color2;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            //create nice brush
            Point p2 = position;
            p2.Offset(0, range.tb.CharHeight);
            var brush = new LinearGradientBrush(position, p2, color1, color2);
            //highlight line
            //if user has selected diapson, draw only line
            if (range.tb.Selection.Start != range.tb.Selection.End)
            {
                if (range.tb.Selection.Start.iLine == range.tb.Selection.End.iLine)
                    gr.DrawLine(new Pen(color2), position.X, position.Y + range.tb.CharHeight, position.X + range.tb.ClientSize.Width, position.Y + range.tb.CharHeight);
            }
            else
                //draw filled rectangle
                gr.FillRectangle(brush, position.X, position.Y, range.tb.ClientSize.Width, range.tb.CharHeight);
        }
    }

    public class InvisibleCharsRenderer : Style
    {
        Pen pen;

        public InvisibleCharsRenderer(Pen pen)
        {
            this.pen = pen;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            var tb = range.tb;
            Brush brush = new SolidBrush(pen.Color);
            foreach (var place in range)
            {
                switch (tb[place].c)
                {
                    case ' ':
                        {
                            var point = tb.PlaceToPoint(place);
                            point.Offset(tb.CharWidth / 2, tb.CharHeight / 2);
                            gr.DrawLine(pen, point.X, point.Y, point.X + 1, point.Y);
                            if (tb[place.iLine].Count - 1 == place.iChar)
                               goto default;
                            break;
                        }
                    default:
                        if (tb[place.iLine].Count - 1 == place.iChar)
                        {
                            var point = tb.PlaceToPoint(place);
                            point.Offset(tb.CharWidth, 0);
                            gr.DrawString("¶", tb.Font, brush, point);
                        }
                        break;
                }
            }
        }
    }
}
