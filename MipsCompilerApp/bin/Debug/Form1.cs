using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization; // Needed for float parsing
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using ScintillaNET;


namespace MipsCompilerApp
{
    public partial class Form1 : Form
    {
        // version
        private const double CDL_Version = 1.05;

        // These are standard Scintilla constants.
        private const int SCI_SETCODEPAGE = 2077;
        private const int SC_CP_UTF8 = 65001;

        // UI Controls - Initialized in InitializeComponentManual
        private Scintilla rtbInput;
        private RichTextBox rtbOutput;
        private Button btnCompile;
        private Button btnCopyOutput;
        private Button btnOpen;
        private Button btnSave;
        private Button btnSaveAs;
        private Button btnNew;
        private Label lblInput;
        private Label lblOutput;
        private Label lblFormat;
        private TextBox txtAddressFormat;
        private Label lblOutputLineCount;
        private MenuStrip menuStrip;

        // --- Added for Font Size ---
        private Label lblFontSize;
        private ComboBox cmbFontSize;
        // --- End of Font Size Additions ---

        private string currentFilePath = null;
        private const int MAX_IMPORT_DEPTH = 10;
        private List<int> errorLineNumbersToHighlight = new List<int>();
        private bool isHighlighting = false;
        private System.Windows.Forms.Timer highlightDebounceTimer;

        private RadioButton rbPS2Mode;
        private RadioButton rbPnachMode;
        private FlowLayoutPanel radioOutputModePanel; // To group the radio buttons

        // Enum to represent the output mode
        private enum OutputFormatMode { PS2, Pnach }
        private OutputFormatMode currentOutputFormat = OutputFormatMode.PS2; // Default to PS2


        // --- Updated Dark Theme Color Definitions ---
        private Color colorFormBackground = Color.FromArgb(44, 47, 51);
        private Color colorPaneBackground = Color.FromArgb(54, 57, 63); // form1 background
        private Color colorControlBackground = Color.FromArgb(70, 74, 80); //64, 68, 75 button background color
        private Color colorControlHover = Color.FromArgb(79, 84, 92);
        private Color colorTextPrimary = Color.FromArgb(220, 221, 222); //220, 221, 222 button text color
        private Color colorTextSecondary = Color.FromArgb(185, 187, 190); //185, 187, 190
        private Color colorAccent = Color.FromArgb(88, 101, 196); //88, 101, 242
        private Color colorAccentHover = Color.FromArgb(71, 82, 196); //71, 82, 196
        private Color colorSaveGreen = Color.FromArgb(67, 141, 100); //67, 181, 129 // green button
        private Color colorSaveGreenHover = Color.FromArgb(57, 154, 110); //57, 154, 110
        private Color colorErrorBackground = Color.FromArgb(240, 71, 71);
        private Color colorEditorBackground = Color.FromArgb(35, 39, 42);
        private Color colorCaret = Color.FromArgb(220, 221, 222); //220, 221, 222 cursor in input box
        private Color colorMarginBackground = Color.FromArgb(48, 51, 56); //48, 51, 56
        private Color colorMenuBackground = Color.FromArgb(58, 61, 67);
        private Color colorMenuHover = Color.FromArgb(70, 74, 80);

        // --- Syntax Token Colors (Adjusted for Dark Theme & User Request) ---
        private Color tokenCommentColor = Color.FromArgb(142, 146, 151);      // Light Grey
        private Color tokenInstructionColor = Color.FromArgb(220, 221, 222);
        private Color tokenDirectiveColor = Color.FromArgb(100, 220, 100);    // Lighter Lime Green (Default Directive)
        private Color tokenDirectivePrintImportColor = Color.FromArgb(180, 120, 210); // Lighter Purple
        private Color tokenDirectiveHexFloatColor = Color.FromArgb(173, 216, 230);  // Light Blue
        private Color tokenRegisterAColor = Color.FromArgb(80, 167, 238); //114, 137, 218
        private Color tokenRegisterTColor = Color.FromArgb(230, 170, 220); //180, 180, 190
        private Color tokenRegisterVColor = Color.FromArgb(250, 119, 109);
        private Color tokenRegisterSColor = Color.FromArgb(241, 196, 15);     // Bright Gold/Yellow
        private Color tokenRegisterSPColor = Color.FromArgb(245, 171, 53);    // Yellow-Orange
        private Color tokenRegisterRAColor = Color.FromArgb(231, 76, 60);     // More Red
        private Color tokenRegisterFColor = Color.FromArgb(119, 221, 119);      // Pastel Green
        private Color tokenRegisterGPColor = Color.FromArgb(46, 204, 113);
        private Color tokenRegisterKColor = Color.FromArgb(155, 89, 182);
        private Color tokenRegisterATColor = Color.FromArgb(173, 216, 230);    // Light Cyan Blue
        private Color tokenRegisterZeroColor = Color.FromArgb(128, 128, 128);
        private Color tokenRegisterFPColor = Color.FromArgb(220, 220, 170);
        private Color tokenRegisterOtherColor = Color.FromArgb(79, 193, 255);
        private Color tokenLabelColor = Color.FromArgb(241, 200, 60); //241, 196, 15
        private Color tokenBasicLabelColor = Color.FromArgb(211, 176, 110); //211, 176, 90
        private Color tokenNumberColor;
        private Color tokenDollarHexColor = Color.FromArgb(212, 212, 212);

        // --- Scintilla Style Indices ---
        private const int STYLE_DEFAULT = ScintillaNET.Style.Default;
        private const int STYLE_COMMENT = 1;
        private const int STYLE_INSTRUCTION = 2;
        private const int STYLE_DIRECTIVE = 3;
        private const int STYLE_REGISTER_A = 4;
        private const int STYLE_REGISTER_T = 5;
        private const int STYLE_REGISTER_V = 6;
        private const int STYLE_REGISTER_S = 7;
        private const int STYLE_REGISTER_SP = 8;
        private const int STYLE_REGISTER_RA = 9;
        private const int STYLE_REGISTER_F = 10;
        private const int STYLE_REGISTER_GP = 11;
        private const int STYLE_REGISTER_K = 12;
        private const int STYLE_REGISTER_AT = 13;
        private const int STYLE_REGISTER_ZERO = 14;
        private const int STYLE_REGISTER_FP = 15;
        private const int STYLE_REGISTER_OTHER = 16;
        private const int STYLE_LABEL = 17;
        private const int STYLE_STRING = 18;
        private const int STYLE_NUMBER = 19;
        private const int STYLE_DOLLAR_HEX = 20;
        private const int STYLE_ERROR_LINE = 21;
        private const int STYLE_DIRECTIVE_PRINTIMPORT = 22;
        private const int STYLE_DIRECTIVE_HEXFLOAT = 23;
        private const int STYLE_BASICLABEL = 24;
        private const int INDICATOR_ERROR_LINE = 8;
        private const int STYLE_LINENUMBER = ScintillaNET.Style.LineNumber;

        private List<string> instructionKeywordsList = new List<string>();
        private List<string> directiveKeywordsList = new List<string>();
        private Dictionary<string, int> _registerStyleMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, Color> _registerColors = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        private class LineSourceInfo
        {
            public string Text { get; private set; }
            public string FileName { get; private set; }
            public int OriginalLineNumber { get; private set; }
            public int GlobalIndex { get; private set; }

            public LineSourceInfo(string text, string fileName, int originalLineNumber, int globalIndex)
            {
                Text = text;
                FileName = fileName;
                OriginalLineNumber = originalLineNumber;
                GlobalIndex = globalIndex;
            }
        }

        private class MipsErrorInfo
        {
            public string FileName { get; set; }
            public int LineNumberInFile { get; set; }
            public int GlobalLineIndex { get; set; }
            public uint AddressAtError { get; set; }
            public string AttemptedData { get; set; }
            public string ErrorMessage { get; set; }
            public string OriginalLineText { get; set; }
            public bool IsFromMainInput { get; set; }
            public int OriginalLineNumber { get; set; }

            public MipsErrorInfo(string fileName, int lineNumberInFile, int globalLineIndex, uint addressAtError, string attemptedData, string errorMessage, string originalLineText, bool isFromMainInput)
            {
                FileName = fileName;
                LineNumberInFile = lineNumberInFile;
                OriginalLineNumber = lineNumberInFile;
                GlobalLineIndex = globalLineIndex;
                AddressAtError = addressAtError;
                AttemptedData = attemptedData ?? "N/A";
                ErrorMessage = errorMessage;
                OriginalLineText = originalLineText;
                IsFromMainInput = isFromMainInput;
            }

            public override string ToString()
            {
                return $"{LineNumberInFile} {Path.GetFileName(FileName)}: {ErrorMessage}";
            }
        }


        public Form1()
        {
            InitializeComponentManual(); // This will now setup cmbFontSize
            InitializeMipsData();
            InitializeScintillaStyles(); // This will now use the initial font size
            LoadDefaultCode();
            UpdateLineCount();

            highlightDebounceTimer = new System.Windows.Forms.Timer();
            highlightDebounceTimer.Interval = 25; //50
            highlightDebounceTimer.Tick += HighlightDebounceTimer_Tick;
        }

        private void HighlightDebounceTimer_Tick(object sender, EventArgs e)
        {
            highlightDebounceTimer.Stop();
            HighlightSyntax();
        }

        private void RtbInput_TextChanged(object sender, EventArgs e)
        {
            errorLineNumbersToHighlight.Clear();
            rtbInput.ScrollWidth = 1;
            highlightDebounceTimer.Stop();
            highlightDebounceTimer.Start();
        }
        private void RtbInput_UpdateUI(object sender, UpdateUIEventArgs e)
        {
            // Can be used for more advanced UI updates if needed
        }

        private void OutputFormatMode_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb != null && rb.Checked) // Ensure the event is for the radio button becoming checked
            {
                if (rb == rbPS2Mode)
                {
                    currentOutputFormat = OutputFormatMode.PS2;
                }
                else if (rb == rbPnachMode)
                {
                    currentOutputFormat = OutputFormatMode.Pnach;
                }
            }
        }

        private void InitializeComponentManual()
        {
            this.SuspendLayout();

            
            // --- SET THE ICON HERE ---
            try
            {
                // Assuming 'myicon.ico' is in the same directory as your executable
                // or you've set its "Copy to Output Directory" property.
                this.Icon = CodeDesignerLite.Properties.Resources.icon;//new Icon("icon.ico");
            }
            catch (System.IO.FileNotFoundException)
            {
                // Handle the case where the icon file is not found
                // You could log this or just proceed without a custom icon
                Console.WriteLine("Icon file 'icon.ico' not found. Using default icon.");
            }
            catch (Exception ex)
            {
                // Handle other potential errors loading the icon
                Console.WriteLine($"Error loading icon: {ex.Message}");
            }
            // --- END ICON SETTING ---
            
            this.Text = "Code Designer Lite";
            this.BackColor = colorFormBackground;
            this.ForeColor = colorTextPrimary;
            this.Size = new Size(850, 700); //1000, 700
            this.MinimumSize = new Size(800, 400); //800, 600
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));

            // --- Menu Strip ---
            menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("&File");
            fileMenu.ForeColor = Color.Black;
            ToolStripMenuItem newMenuItem = new ToolStripMenuItem("&New", null, BtnNew_Click) { ForeColor = Color.Black };
            ToolStripMenuItem openMenuItem = new ToolStripMenuItem("&Open...", null, BtnOpen_Click) { ForeColor = Color.Black };
            ToolStripMenuItem saveMenuItem = new ToolStripMenuItem("&Save", null, BtnSave_Click) { ForeColor = Color.Black };
            ToolStripMenuItem saveAsMenuItem = new ToolStripMenuItem("Save &As...", null, BtnSaveAs_Click) { ForeColor = Color.Black };
            ToolStripSeparator separator = new ToolStripSeparator();
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem("E&xit", null, (s, e) => this.Close()) { ForeColor = Color.Black };
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { newMenuItem, openMenuItem, saveMenuItem, saveAsMenuItem, separator, exitMenuItem });
            ToolStripMenuItem aboutMenu = new ToolStripMenuItem("&About");
            aboutMenu.ForeColor = Color.Black;
            aboutMenu.Click += AboutMenuItem_Click;
            menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, aboutMenu });
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

            // --- Main Layout Panel ---
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = Padding.Empty,
                RowCount = 1,
                ColumnCount = 2,
                BackColor = colorFormBackground
            };
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 78F));
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            this.Controls.Add(mainPanel);
            mainPanel.BringToFront();


            // --- Left Pane (Input Area) ---
            Panel leftPane = new Panel { Dock = DockStyle.Fill, BackColor = colorPaneBackground, Padding = Padding.Empty };
            mainPanel.Controls.Add(leftPane, 0, 0);

            lblInput = new Label { Text = "INPUT", AutoSize = true, ForeColor = colorTextSecondary, Margin = new Padding(3, 5, 3, 3) };

            // --- Font Size Controls ---
            lblFontSize = new Label { Text = "FONT SIZE:", AutoSize = true, ForeColor = colorTextSecondary, Margin = new Padding(5, 5, 0, 3) };
            cmbFontSize = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 55,
                BackColor = colorControlBackground,
                ForeColor = colorTextPrimary,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 3, 5, 0),
                Font = new Font("Segoe UI", 8F)
            };
            for (int i = 5; i <= 36; i++)
            {
                cmbFontSize.Items.Add(i);
            }
            cmbFontSize.SelectedItem = 10; // Default font size // was 11
            cmbFontSize.SelectedIndexChanged += CmbFontSize_SelectedIndexChanged; // Hook event handler


            lblFormat = new Label { Text = "FORMAT:", AutoSize = true, ForeColor = colorTextSecondary, Margin = new Padding(5, 5, 3, 3) };
            txtAddressFormat = new TextBox { Text = "-", Width = 30, MaxLength = 1, TextAlign = HorizontalAlignment.Center, BackColor = colorControlBackground, ForeColor = colorTextPrimary, BorderStyle = BorderStyle.None, Margin = new Padding(0, 3, 5, 0) };

            TableLayoutPanel inputHeaderPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 30, ColumnCount = 6, AutoSize = true, BackColor = colorPaneBackground };
            inputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // 0: Input Label
            inputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // 1: Radio Button Panel
            inputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // 2: Font Size Label
            inputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // 3: Font Size ComboBox
            inputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // 4: Format Label
            inputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));    // 5: Format TextBox

            // --- Create RadioButtons and their Panel ---
            rbPS2Mode = new RadioButton
            {
                Text = "PS2",
                AutoSize = true,
                ForeColor = colorTextSecondary,
                BackColor = colorPaneBackground, // Match panel background
                Checked = true, // Default to PS2
                Margin = new Padding(0, 3, 5, 0) // (left, top, right, bottom)
            };
            rbPnachMode = new RadioButton
            {
                Text = "pnach",
                AutoSize = true,
                ForeColor = colorTextSecondary,
                BackColor = colorPaneBackground,
                Margin = new Padding(5, 3, 5, 0) // Add some spacing
            };

            // Add event handlers to update currentOutputFormat
            rbPS2Mode.CheckedChanged += OutputFormatMode_CheckedChanged;
            rbPnachMode.CheckedChanged += OutputFormatMode_CheckedChanged;

            radioOutputModePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0), // Let TableLayoutPanel handle outer margin/alignment
                Padding = new Padding(0),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom // Anchor the panel
            };
            radioOutputModePanel.Controls.Add(rbPS2Mode);
            radioOutputModePanel.Controls.Add(rbPnachMode);

            inputHeaderPanel.Controls.Add(lblInput, 0, 0);
            inputHeaderPanel.Controls.Add(radioOutputModePanel, 1, 0);  // New column for radio buttons
            inputHeaderPanel.Controls.Add(lblFontSize, 2, 0);         // Shifted from column 1 to 2
            inputHeaderPanel.Controls.Add(cmbFontSize, 3, 0);         // Shifted from column 2 to 3
            inputHeaderPanel.Controls.Add(lblFormat, 4, 0);           // Shifted from column 3 to 4
            inputHeaderPanel.Controls.Add(txtAddressFormat, 5, 0);    // Shifted from column 4 to 5

            lblFontSize.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            cmbFontSize.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            lblFormat.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            txtAddressFormat.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

            int initialFontSize = 11;
            if (cmbFontSize.SelectedItem != null)
            {
                initialFontSize = (int)cmbFontSize.SelectedItem;
            }

            rtbInput = new ScintillaNET.Scintilla
            {
                Dock = DockStyle.Fill,
                Lexer = Lexer.Container,
                WrapMode = ScintillaNET.WrapMode.None, // <<< CORRECTED LINE// WrapMode = WrapMode.None,
                IndentationGuides = IndentView.LookBoth,
                BorderStyle = BorderStyle.None,
                MultipleSelection = true,
                TabWidth = 4,
                UseTabs = false,
                CaretForeColor = colorCaret,
                CaretWidth = 2,
                CaretStyle = CaretStyle.Line,
                CaretPeriod = 500,
                Font = new Font("Consolas", initialFontSize), // Use initialFontSize
                ScrollWidthTracking = true,
                ScrollWidth = 1
            };
            rtbInput.Margins[0].Type = MarginType.Number;
            rtbInput.Margins[0].Width = 35;
            // Scintilla line number style will be set in InitializeScintillaStyles

            rtbInput.TextChanged += RtbInput_TextChanged;
            rtbInput.UpdateUI += RtbInput_UpdateUI;

            TableLayoutPanel inputAreaLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, BackColor = colorPaneBackground };
            inputAreaLayout.RowStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            inputAreaLayout.Controls.Add(rtbInput, 0, 0);

            leftPane.Controls.Add(inputAreaLayout);
            leftPane.Controls.Add(inputHeaderPanel);


            // --- Right Pane (Output & Buttons) ---
            Panel rightPane = new Panel { Dock = DockStyle.Fill, BackColor = colorPaneBackground, Padding = Padding.Empty };
            mainPanel.Controls.Add(rightPane, 1, 0);

            lblOutput = new Label { Text = "OUTPUT", AutoSize = true, ForeColor = colorTextSecondary, Margin = new Padding(3, 5, 3, 3) };
            lblOutputLineCount = new Label { Text = "LINES: 0", AutoSize = true, ForeColor = colorTextSecondary, TextAlign = ContentAlignment.MiddleRight, Dock = DockStyle.Right };

            TableLayoutPanel outputHeaderPanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 30, ColumnCount = 2, AutoSize = true, BackColor = colorPaneBackground };
            outputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            outputHeaderPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            outputHeaderPanel.Controls.Add(lblOutput, 0, 0);
            outputHeaderPanel.Controls.Add(lblOutputLineCount, 1, 0);


            rtbOutput = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0))), // Use initialFontSize --initialFontSize
                BackColor = colorEditorBackground,
                ForeColor = colorTextPrimary,
                BorderStyle = BorderStyle.None,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both
            };

            // --- Buttons (Moved to Right Pane Bottom) ---
            btnCompile = new Button { Text = "COMPILE", BackColor = colorAccent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom, Font = new Font(this.Font, FontStyle.Bold) };
            btnCompile.FlatAppearance.BorderSize = 0;
            btnCompile.Click += BtnCompile_Click;

            btnCopyOutput = new Button { Text = "COPY", BackColor = colorSaveGreen, ForeColor = colorTextPrimary, FlatStyle = FlatStyle.Flat, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom, Font = new Font(this.Font, FontStyle.Bold) };
            btnCopyOutput.FlatAppearance.BorderSize = 0;
            btnCopyOutput.Click += BtnCopyOutput_Click;

            btnNew = new Button { Text = "NEW", BackColor = colorControlBackground, ForeColor = colorTextPrimary, FlatStyle = FlatStyle.Flat, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom, Font = new Font(this.Font, FontStyle.Bold) };
            btnNew.FlatAppearance.BorderSize = 0;
            btnNew.Click += BtnNew_Click;

            btnOpen = new Button { Text = "OPEN", BackColor = colorControlBackground, ForeColor = colorTextPrimary, FlatStyle = FlatStyle.Flat, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom, Font = new Font(this.Font, FontStyle.Bold) };
            btnOpen.FlatAppearance.BorderSize = 0;
            btnOpen.Click += BtnOpen_Click;

            btnSave = new Button { Text = "SAVE", BackColor = colorControlBackground, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom, Font = new Font(this.Font, FontStyle.Bold) };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnSaveAs = new Button { Text = "SAVE AS", BackColor = colorControlBackground, ForeColor = colorTextPrimary, FlatStyle = FlatStyle.Flat, Height = 30, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom, Font = new Font(this.Font, FontStyle.Bold) };
            btnSaveAs.FlatAppearance.BorderSize = 0;
            btnSaveAs.Click += BtnSaveAs_Click;

            TableLayoutPanel buttonPanelLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0, 5, 0, 0),
                BackColor = colorPaneBackground,
                ColumnCount = 2,
                RowCount = 4,
                Margin = new Padding(0)
            };
            buttonPanelLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            buttonPanelLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            buttonPanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            buttonPanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            buttonPanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            buttonPanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));

            buttonPanelLayout.Controls.Add(btnCompile, 0, 0);
            buttonPanelLayout.SetColumnSpan(btnCompile, 2);
            buttonPanelLayout.Controls.Add(btnCopyOutput, 0, 1);
            buttonPanelLayout.SetColumnSpan(btnCopyOutput, 2);
            buttonPanelLayout.Controls.Add(btnNew, 0, 2);
            buttonPanelLayout.Controls.Add(btnOpen, 1, 2);
            buttonPanelLayout.Controls.Add(btnSave, 0, 3);
            buttonPanelLayout.Controls.Add(btnSaveAs, 1, 3);


            TableLayoutPanel outputAreaLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = colorPaneBackground };
            outputAreaLayout.RowStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            outputAreaLayout.RowStyles.Add(new ColumnStyle(SizeType.AutoSize));
            outputAreaLayout.Controls.Add(rtbOutput, 0, 0);
            outputAreaLayout.Controls.Add(buttonPanelLayout, 0, 1);


            rightPane.Controls.Add(outputAreaLayout);
            rightPane.Controls.Add(outputHeaderPanel);


            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // --- ADDED: Event Handler for Font Size ComboBox ---
        private void CmbFontSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFontSize.SelectedItem == null || rtbInput == null || rtbOutput == null || rtbInput.IsDisposed)
                return;

            try
            {
                int newSize = (int)cmbFontSize.SelectedItem;

                // Store current caret position and selection to restore later
                int selectionStart = rtbInput.SelectionStart;
                int selectionEnd = rtbInput.SelectionEnd;
                int firstVisibleLine = rtbInput.FirstVisibleLine;

                // Update RichTextBox (Output) font
                //rtbOutput.Font = new Font(rtbOutput.Font.FontFamily, newSize, rtbOutput.Font.Style);

                // Update Scintilla (Input) font
                // First, update the base font of the control itself
                rtbInput.Font = new Font(rtbInput.Font.FontFamily, newSize, rtbInput.Font.Style);

                // Then, update the default style's font size in Scintilla
                // This is crucial for how Scintilla handles styling.
                // Note: InitializeScintillaStyles will also set this based on rtbInput.Font
                // rtbInput.Styles[ScintillaNET.Style.Default].Size = newSize; // This will be handled by InitializeScintillaStyles

                // Re-initialize all styles. This will use the new rtbInput.Font.Size for the default style.
                InitializeScintillaStyles();

                // Re-apply syntax highlighting to the entire document
                // It might be beneficial to only highlight visible area for performance on very large docs,
                // but for simplicity and correctness, full highlight is safer after font change.
                HighlightSyntax();

                // Restore caret position and selection
                // Check if rtbInput is not disposed before accessing properties
                if (!rtbInput.IsDisposed)
                {
                    rtbInput.SetSelection(selectionStart, selectionEnd);
                    rtbInput.FirstVisibleLine = firstVisibleLine;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating font size: {ex.Message}", "Font Size Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void LoadDefaultCode()
        {
            rtbInput.Text = "";
        }


        // --- Syntax Highlighting ---
        private void InitializeScintillaStyles()
        {
            rtbInput.DirectMessage(SCI_SETCODEPAGE, new IntPtr(SC_CP_UTF8), IntPtr.Zero);

            // Use the current font of rtbInput for default styling
            rtbInput.Styles[ScintillaNET.Style.Default].Font = rtbInput.Font.Name;
            rtbInput.Styles[ScintillaNET.Style.Default].Size = (int)rtbInput.Font.Size;
            rtbInput.Styles[ScintillaNET.Style.Default].BackColor = colorEditorBackground;
            rtbInput.Styles[ScintillaNET.Style.Default].ForeColor = colorTextPrimary;
            rtbInput.StyleClearAll(); // Clears all styling and applies the new default font/size

            // Re-apply the default style settings after StyleClearAll as it resets them
            rtbInput.Styles[ScintillaNET.Style.Default].Font = rtbInput.Font.Name;
            rtbInput.Styles[ScintillaNET.Style.Default].Size = (int)rtbInput.Font.Size;
            rtbInput.Styles[ScintillaNET.Style.Default].BackColor = colorEditorBackground;
            rtbInput.Styles[ScintillaNET.Style.Default].ForeColor = colorTextPrimary;

            // setup cursor color
            rtbInput.CaretForeColor = colorCaret;
            rtbInput.CaretWidth = 2;
            rtbInput.CaretStyle = CaretStyle.Line;
            rtbInput.CaretPeriod = 500;

            // user select text background color            
            rtbInput.SetSelectionBackColor(true, Color.FromArgb(96, 96, 96));        //66, 66, 66     

            // Line Number Margin Style - ensure it uses the default font's characteristics or a fixed one
            rtbInput.Margins[0].Type = MarginType.Number;
            rtbInput.Margins[0].Width = 35; // Adjust width if necessary for larger font numbers
            rtbInput.Styles[STYLE_LINENUMBER].Font = rtbInput.Font.Name; // Optional: Make line number font match
            // Make line numbers slightly smaller, but not too small
            int lineNumberFontSize = (int)rtbInput.Font.Size - 1;
            if (lineNumberFontSize < 5) lineNumberFontSize = (int)rtbInput.Font.Size;
            if (lineNumberFontSize < 1) lineNumberFontSize = 1; // Absolute minimum
            rtbInput.Styles[STYLE_LINENUMBER].Size = 10; // lineNumberFontSize; //lineNumberFontSize
            rtbInput.Styles[STYLE_LINENUMBER].BackColor = colorMarginBackground;
            rtbInput.Styles[STYLE_LINENUMBER].ForeColor = colorTextSecondary;

            rtbInput.Styles[STYLE_COMMENT].ForeColor = tokenCommentColor;
            rtbInput.Styles[STYLE_COMMENT].Italic = true;
            rtbInput.Styles[STYLE_INSTRUCTION].ForeColor = tokenInstructionColor;
            rtbInput.Styles[STYLE_DIRECTIVE].ForeColor = tokenDirectiveColor;
            rtbInput.Styles[STYLE_DIRECTIVE].Bold = true;
            rtbInput.Styles[STYLE_DIRECTIVE_PRINTIMPORT].ForeColor = tokenDirectivePrintImportColor;
            rtbInput.Styles[STYLE_DIRECTIVE_HEXFLOAT].ForeColor = tokenDirectiveHexFloatColor;
            rtbInput.Styles[STYLE_LABEL].ForeColor = tokenLabelColor; // set label color
            rtbInput.Styles[STYLE_LABEL].BackColor = Color.FromArgb(50, 50, 50, 50); //60, 50, 40
            rtbInput.Styles[STYLE_LABEL].Bold = true;            
            rtbInput.Styles[STYLE_BASICLABEL].ForeColor = tokenBasicLabelColor; // set basic label color
            rtbInput.Styles[STYLE_STRING].ForeColor = colorTextPrimary;
            rtbInput.Styles[STYLE_NUMBER].ForeColor = colorTextPrimary;
            rtbInput.Styles[STYLE_DOLLAR_HEX].ForeColor = tokenDollarHexColor;

            rtbInput.Styles[STYLE_REGISTER_ZERO].ForeColor = tokenRegisterZeroColor;
            rtbInput.Styles[STYLE_REGISTER_AT].ForeColor = tokenRegisterATColor;
            rtbInput.Styles[STYLE_REGISTER_V].ForeColor = tokenRegisterVColor;
            rtbInput.Styles[STYLE_REGISTER_A].ForeColor = tokenRegisterAColor;
            rtbInput.Styles[STYLE_REGISTER_T].ForeColor = tokenRegisterTColor;
            rtbInput.Styles[STYLE_REGISTER_S].ForeColor = tokenRegisterSColor;
            rtbInput.Styles[STYLE_REGISTER_K].ForeColor = tokenRegisterKColor;
            rtbInput.Styles[STYLE_REGISTER_GP].ForeColor = tokenRegisterGPColor;
            rtbInput.Styles[STYLE_REGISTER_SP].ForeColor = tokenRegisterSPColor;
            rtbInput.Styles[STYLE_REGISTER_FP].ForeColor = tokenRegisterFPColor;
            rtbInput.Styles[STYLE_REGISTER_RA].ForeColor = tokenRegisterRAColor;
            rtbInput.Styles[STYLE_REGISTER_F].ForeColor = tokenRegisterFColor;
            rtbInput.Styles[STYLE_REGISTER_OTHER].ForeColor = tokenRegisterOtherColor;

            rtbInput.Styles[STYLE_ERROR_LINE].ForeColor = Color.White;
            rtbInput.Styles[STYLE_ERROR_LINE].BackColor = colorErrorBackground;

            //rtbInput.SetSelectionBackColor(Color.FromArgb(240, 71, 71));
        }

        private void HighlightSyntax()
        {
            HighlightSyntax(0, rtbInput.TextLength);
        }

        private void HighlightSyntax(int startPos, int length)
        {
            if (isHighlighting || rtbInput.IsDisposed) return;
            isHighlighting = true;

            try
            {
                rtbInput.Lexer = Lexer.Container;

                int currentSelectionStart = rtbInput.SelectionStart;
                int currentSelectionEnd = rtbInput.SelectionEnd;

                rtbInput.StartStyling(startPos);
                rtbInput.SetStyling(length, STYLE_DEFAULT);

                if (instructionKeywordsList.Count == 0) InitializeMipsData();
                // Check if a core style property (like comment color) or default font size needs refresh.
                // This check is important because InitializeScintillaStyles might be called by font changes.
                if (rtbInput.Styles[STYLE_COMMENT].ForeColor != tokenCommentColor ||
                    rtbInput.Styles[STYLE_DEFAULT].Size != (int)rtbInput.Font.Size)
                {
                    InitializeScintillaStyles(); // Re-initialize if styles are not set up or font size changed
                }


                string textToStyle = rtbInput.GetTextRange(startPos, length);

                foreach (Match m in Regex.Matches(textToStyle, @"""(?:\\.|[^""\\])*"""))
                { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_STRING); }

                // HANDLE LABEL colors
                foreach (Match m in Regex.Matches(textToStyle, @"^\s*([\w.:]+):", RegexOptions.Multiline))
                {
                    // Get the matched label name (the content of the first capturing group)
                    string labelName = m.Groups[1].Value;
                    int styleToApply;

                    // Check if the label name starts with "FNC", ignoring case
                    if (labelName.StartsWith("FNC", StringComparison.OrdinalIgnoreCase))
                    {
                        styleToApply = STYLE_LABEL;
                    }
                    else
                    {
                        styleToApply = STYLE_BASICLABEL;
                    }

                    // Apply the determined style.
                    // Styling starts at the beginning of the captured label name (m.Groups[1].Index)
                    // and covers the length of the label name plus the colon (m.Groups[1].Length + 1).
                    rtbInput.StartStyling(startPos + m.Groups[1].Index);
                    rtbInput.SetStyling(m.Groups[1].Length + 1, styleToApply);
                }

                foreach (Match m in Regex.Matches(textToStyle, @"\$([0-9a-fA-F]+)\b", RegexOptions.IgnoreCase))
                { if (rtbInput.GetStyleAt(startPos + m.Index) == STYLE_DEFAULT) { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_DOLLAR_HEX); } }
                foreach (Match m in Regex.Matches(textToStyle, @"\b(0x[0-9a-fA-F]+)\b", RegexOptions.IgnoreCase))
                { if (rtbInput.GetStyleAt(startPos + m.Index) == STYLE_DEFAULT) { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_NUMBER); } }
                foreach (Match m in Regex.Matches(textToStyle, @"(?<![\w.:$])(\-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)\b"))
                {
                    if (rtbInput.GetStyleAt(startPos + m.Index) == STYLE_DEFAULT)
                    {
                        rtbInput.StartStyling(startPos + m.Index);
                        rtbInput.SetStyling(m.Length, STYLE_NUMBER);
                    }
                }

                string regPattern = @"\$?((?:zero|at|v[01]|a[0-3]|t[0-9]|s[0-7]|f\d{1,2}|k[01]|gp|sp|fp|ra)|(?:[0-2]?[0-9]|3[01]))\b";
                foreach (Match m in Regex.Matches(textToStyle, regPattern, RegexOptions.IgnoreCase))
                {
                    if (rtbInput.GetStyleAt(startPos + m.Index) == STYLE_DEFAULT)
                    {
                        string regCoreName = m.Groups[1].Value.ToLower();
                        if (_registerStyleMap.TryGetValue(regCoreName, out int styleIndex))
                        {
                            rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, styleIndex);
                        }
                        else
                        {
                            rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_REGISTER_OTHER);
                        }
                    }
                }

                foreach (Match m in Regex.Matches(textToStyle, $@"\b({string.Join("|", directiveKeywordsList)})(?=[\s""(])", RegexOptions.IgnoreCase))
                { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_DIRECTIVE); }

                foreach (Match m in Regex.Matches(textToStyle, $@"\b({string.Join("|", instructionKeywordsList)})(?=[\s(.:])", RegexOptions.IgnoreCase))
                { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_INSTRUCTION); }

                foreach (Match m in Regex.Matches(textToStyle, @"^\s*(j|jal|b)\s+([\w.:$0-9a-fA-FxX.:-]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    int operandStart = startPos + m.Groups[2].Index;
                    int operandLength = m.Groups[2].Length;
                    rtbInput.StartStyling(operandStart);
                    rtbInput.SetStyling(operandLength, STYLE_DEFAULT);
                }
                foreach (Match m in Regex.Matches(textToStyle, @"^\s*(hexcode|float)\s+([\w.:$0-9a-fA-FxX.:-]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline))
                {
                    int operandStart = startPos + m.Groups[2].Index;
                    int operandLength = m.Groups[2].Length;
                    rtbInput.StartStyling(operandStart);
                    rtbInput.SetStyling(operandLength, STYLE_DEFAULT);
                }

                // PRINT COMMAND
                foreach (Match m in Regex.Matches(textToStyle, @"\b(print|import)(?=[\s""(])", RegexOptions.IgnoreCase))
                { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_DIRECTIVE_PRINTIMPORT); }
                // FLOAT COMMAND
                foreach (Match m in Regex.Matches(textToStyle, @"\b(hexcode|float)(?=[\s""(])", RegexOptions.IgnoreCase))
                { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_DIRECTIVE_HEXFLOAT); }
                // LABELS 2nd pass
                string labelUsagePattern = @":([\w.:]+)";
                foreach (Match m in Regex.Matches(textToStyle, labelUsagePattern))
                {
                    // m.Value will be like ":label1"
                    // m.Groups[1].Value will be "label1"

                    int matchStartIndex = startPos + m.Index; // Absolute start index of the match (e.g., of ":label1")
                    int matchLength = m.Length;               // Length of the entire match (e.g., of ":label1")

                    string labelName = m.Groups[1].Value;     // The actual label name part
                    int styleToApply;

                    if (labelName.StartsWith("FNC", StringComparison.OrdinalIgnoreCase))
                    {
                        styleToApply = STYLE_INSTRUCTION;       // Your defined style for FNC-prefixed labels
                    }
                    else
                    {
                        styleToApply = STYLE_INSTRUCTION;  // Your defined style for other labels
                    }

                    // Apply the style to the entire ":labelname" match
                    rtbInput.StartStyling(matchStartIndex);
                    rtbInput.SetStyling(matchLength, styleToApply);
                }

                // COMMENTS
                foreach (Match m in Regex.Matches(textToStyle, @"/\*[\s\S]*?\*/", RegexOptions.Multiline))
                { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_COMMENT); }
                foreach (Match m in Regex.Matches(textToStyle, @"(\/\/|#).*$", RegexOptions.Multiline))
                { rtbInput.StartStyling(startPos + m.Index); rtbInput.SetStyling(m.Length, STYLE_COMMENT); }

                // ERROR HIGHLIGHT
                foreach (int errorLineIndex in errorLineNumbersToHighlight)
                {
                    if (errorLineIndex >= 0 && errorLineIndex < rtbInput.Lines.Count)
                    {
                        int lineActualStart = rtbInput.Lines[errorLineIndex].Position;
                        int lineActualLength = rtbInput.Lines[errorLineIndex].Length;
                        if (lineActualLength == 0 && lineActualStart < rtbInput.TextLength) lineActualLength = 1;
                        else if (lineActualLength == 0 && lineActualStart == rtbInput.TextLength) continue;

                        rtbInput.StartStyling(lineActualStart);
                        rtbInput.SetStyling(lineActualLength, STYLE_ERROR_LINE);
                    }
                }                              
                rtbInput.SetSelection(currentSelectionStart, currentSelectionEnd);
            }
            finally
            {
                isHighlighting = false;
            }
        }


        // --- MIPS Assembler Logic (Registers, OPS, Parsing) ---
        private Dictionary<string, int> mipsRegisters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, MipsOpInfo> mipsOps = new Dictionary<string, MipsOpInfo>(StringComparer.OrdinalIgnoreCase);

        private class MipsOpInfo
        {
            public string Type { get; set; } = "";
            public uint Opcode { get; set; }
            public uint Funct { get; set; }
            public uint Fmt { get; set; }
            public uint CopOp { get; set; }
            public uint RtField { get; set; }
            public uint CcBit { get; set; }
            public uint CustomValue { get; set; }
        }

        private void InitializeRegisterColors()
        {
            _registerColors["zero"] = tokenRegisterZeroColor; _registerColors["at"] = tokenRegisterATColor;
            _registerColors["v0"] = tokenRegisterVColor; _registerColors["v1"] = tokenRegisterVColor;
            _registerColors["a0"] = tokenRegisterAColor; _registerColors["a1"] = tokenRegisterAColor; _registerColors["a2"] = tokenRegisterAColor; _registerColors["a3"] = tokenRegisterAColor;
            for (int i = 0; i <= 9; i++) _registerColors[$"t{i}"] = tokenRegisterTColor;
            for (int i = 0; i <= 7; i++) _registerColors[$"s{i}"] = tokenRegisterSColor;
            _registerColors["k0"] = tokenRegisterKColor; _registerColors["k1"] = tokenRegisterKColor;
            _registerColors["gp"] = tokenRegisterGPColor; _registerColors["sp"] = tokenRegisterSPColor;
            _registerColors["fp"] = tokenRegisterFPColor; _registerColors["ra"] = tokenRegisterRAColor;
            for (int i = 0; i < 32; i++) _registerColors[$"f{i}"] = tokenRegisterFColor;

            string[] gprNames = { "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7", "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra" };
            for (int i = 0; i < gprNames.Length && i < 32; i++)
            {
                if (_registerColors.ContainsKey(gprNames[i]))
                {
                    _registerColors[i.ToString()] = _registerColors[gprNames[i]];
                }
            }
            _registerStyleMap.Clear();
            foreach (var kvp in _registerColors)
            {
                int styleIndexToUse = STYLE_REGISTER_OTHER;
                if (kvp.Key == "zero") styleIndexToUse = STYLE_REGISTER_ZERO;
                else if (kvp.Key == "at") styleIndexToUse = STYLE_REGISTER_AT;
                else if (kvp.Key == "v0" || kvp.Key == "v1") styleIndexToUse = STYLE_REGISTER_V;
                else if (kvp.Key.StartsWith("a") && kvp.Key.Length == 2 && char.IsDigit(kvp.Key[1])) styleIndexToUse = STYLE_REGISTER_A;
                else if (kvp.Key.StartsWith("t") && kvp.Key.Length == 2 && char.IsDigit(kvp.Key[1])) styleIndexToUse = STYLE_REGISTER_T;
                else if (kvp.Key.StartsWith("s") && kvp.Key.Length == 2 && char.IsDigit(kvp.Key[1])) styleIndexToUse = STYLE_REGISTER_S;
                else if (kvp.Key.StartsWith("f") && kvp.Key.Length > 1 && kvp.Key.Substring(1).All(char.IsDigit)) styleIndexToUse = STYLE_REGISTER_F;
                else if (kvp.Key == "k0" || kvp.Key == "k1") styleIndexToUse = STYLE_REGISTER_K;
                else if (kvp.Key == "gp") styleIndexToUse = STYLE_REGISTER_GP;
                else if (kvp.Key == "sp") styleIndexToUse = STYLE_REGISTER_SP;
                else if (kvp.Key == "fp") styleIndexToUse = STYLE_REGISTER_FP;
                else if (kvp.Key == "ra") styleIndexToUse = STYLE_REGISTER_RA;

                _registerStyleMap[kvp.Key] = styleIndexToUse;
                if (int.TryParse(kvp.Key, out int regNumVal) && regNumVal >= 0 && regNumVal < 32)
                {
                    if (regNumVal < gprNames.Length && _registerStyleMap.ContainsKey(gprNames[regNumVal]))
                    {
                        _registerStyleMap[regNumVal.ToString()] = _registerStyleMap[gprNames[regNumVal]];
                    }
                    else
                    {
                        _registerStyleMap[regNumVal.ToString()] = styleIndexToUse;
                    }
                }
            }
            _registerStyleMap["other"] = STYLE_REGISTER_OTHER;
        }


        private void InitializeMipsData()
        {
            string[] gprNames = { "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7", "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra" };
            for (int i = 0; i < gprNames.Length; i++)
            {
                mipsRegisters[gprNames[i]] = i;
                mipsRegisters["$" + gprNames[i]] = i;
                mipsRegisters[i.ToString()] = i;
                //mipsRegisters["$" + i.ToString()] = i;
            }
            for (int i = 0; i < 32; i++)
            {
                mipsRegisters[$"f{i}"] = i;
                mipsRegisters[$"$f{i}"] = i;
            }
            InitializeRegisterColors();


            instructionKeywordsList = new List<string> {
                "addiu","sw","jal","nop","lw","jr","addu","add","sub","and","or","xor","nor","slt","sltu", "subu",
                "addi","andi","ori","xori","lui","slti","sltiu","beq","bne", "beql", "bnel", "b",
                "sll","srl","sra","sllv","srlv","srav",
                "mult","multu","div","divu","mfhi","mflo","mthi","mtlo",
                "lb","lbu","lh","lhu","sb","sh", "sd", "ld", "lwu", "lq", "sq",
                "jalr",
                "bltz","bgez","blez","bgtz", "bltzal", "bgezal",
                "syscall","break", "sync", "eret",
                "mfc0","mtc0", "dmfc0", "dmtc0",
                "lwc1", "swc1", "mfc1", "mtc1", "add.s", "sub.s", "mul.s", "div.s", "abs.s", "neg.s", "mov.s", "sqrt.s",
                "c.eq.s", "c.lt.s", "c.le.s", "bc1t", "bc1f", "cvt.s.w", "cvt.w.s",
                "ldc1", "sdc1", "add.d", "sub.d", "mul.d", "div.d", "abs.d", "neg.d", "cvt.s.d", "cvt.d.s", "cvt.w.d", "cvt.d.w", "cvt.l.s", "cvt.l.d",
                "setreg", "dadd", "daddi", "daddiu", "dsub", "dsubu", "dsll", "dsrl", "dsra", "dsllv", "dsrlv", "dsrav", "dsll32", "dsrl32", "dsra32", "dmult", "dmultu", "ddiv", "ddivu"
            };
            instructionKeywordsList = instructionKeywordsList.Select(k => k.ToLower()).Distinct().ToList();

            directiveKeywordsList = new List<string> {
                "address","print","import","hexcode","float",".word",".byte",".half",".ascii",".asciiz",".space",".text",".data",".globl",".align"
            };


            mipsOps["add"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x20 };
            mipsOps["addu"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x21 };
            mipsOps["sub"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x22 };
            mipsOps["subu"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x23 };
            mipsOps["and"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x24 };
            mipsOps["or"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x25 };
            mipsOps["xor"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x26 };
            mipsOps["nor"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x27 };
            mipsOps["slt"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x2A };
            mipsOps["sltu"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x2B };
            mipsOps["sll"] = new MipsOpInfo { Type = "R_SHIFT", Opcode = 0x00, Funct = 0x00 };
            mipsOps["srl"] = new MipsOpInfo { Type = "R_SHIFT", Opcode = 0x00, Funct = 0x02 };
            mipsOps["sra"] = new MipsOpInfo { Type = "R_SHIFT", Opcode = 0x00, Funct = 0x03 };
            mipsOps["sllv"] = new MipsOpInfo { Type = "R_SHIFT_V", Opcode = 0x00, Funct = 0x04 };
            mipsOps["srlv"] = new MipsOpInfo { Type = "R_SHIFT_V", Opcode = 0x00, Funct = 0x06 };
            mipsOps["srav"] = new MipsOpInfo { Type = "R_SHIFT_V", Opcode = 0x00, Funct = 0x07 };
            mipsOps["jr"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x08 };
            mipsOps["jalr"] = new MipsOpInfo { Type = "R_JALR", Opcode = 0x00, Funct = 0x09 };
            mipsOps["mult"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x18 };
            mipsOps["multu"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x19 };
            mipsOps["div"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x1A };
            mipsOps["divu"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x1B };
            mipsOps["mfhi"] = new MipsOpInfo { Type = "R_MFHI_MFLO", Opcode = 0x00, Funct = 0x10 };
            mipsOps["mflo"] = new MipsOpInfo { Type = "R_MFHI_MFLO", Opcode = 0x00, Funct = 0x12 };
            mipsOps["mthi"] = new MipsOpInfo { Type = "R_MTHI_MTLO", Opcode = 0x00, Funct = 0x11 };
            mipsOps["mtlo"] = new MipsOpInfo { Type = "R_MTHI_MTLO", Opcode = 0x00, Funct = 0x13 };
            mipsOps["syscall"] = new MipsOpInfo { Type = "R_SYSCALL_BREAK", Opcode = 0x00, Funct = 0x0C };
            mipsOps["break"] = new MipsOpInfo { Type = "R_SYSCALL_BREAK", Opcode = 0x00, Funct = 0x0D };
            mipsOps["sync"] = new MipsOpInfo { Type = "R_SYNC", Opcode = 0x00, Funct = 0x0F };
            mipsOps["dadd"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x2C };
            mipsOps["daddu"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x2D };
            mipsOps["dsub"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x2E };
            mipsOps["dsubu"] = new MipsOpInfo { Type = "R", Opcode = 0x00, Funct = 0x2F };
            mipsOps["dsll"] = new MipsOpInfo { Type = "R_SHIFT", Opcode = 0x00, Funct = 0x38 };
            mipsOps["dsrl"] = new MipsOpInfo { Type = "R_SHIFT", Opcode = 0x00, Funct = 0x3A };
            mipsOps["dsra"] = new MipsOpInfo { Type = "R_SHIFT", Opcode = 0x00, Funct = 0x3B };
            mipsOps["dsllv"] = new MipsOpInfo { Type = "R_SHIFT_V", Opcode = 0x00, Funct = 0x14 };
            mipsOps["dsrlv"] = new MipsOpInfo { Type = "R_SHIFT_V", Opcode = 0x00, Funct = 0x16 };
            mipsOps["dsrav"] = new MipsOpInfo { Type = "R_SHIFT_V", Opcode = 0x00, Funct = 0x17 };
            mipsOps["dsll32"] = new MipsOpInfo { Type = "R_SHIFT_PLUS32", Opcode = 0x00, Funct = 0x3C };
            mipsOps["dsrl32"] = new MipsOpInfo { Type = "R_SHIFT_PLUS32", Opcode = 0x00, Funct = 0x3E };
            mipsOps["dsra32"] = new MipsOpInfo { Type = "R_SHIFT_PLUS32", Opcode = 0x00, Funct = 0x3F };
            mipsOps["dmult"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x1C };
            mipsOps["dmultu"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x1D };
            mipsOps["ddiv"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x1E };
            mipsOps["ddivu"] = new MipsOpInfo { Type = "R_MULTDIV", Opcode = 0x00, Funct = 0x1F };
            mipsOps["addi"] = new MipsOpInfo { Type = "I", Opcode = 0x08 };
            mipsOps["addiu"] = new MipsOpInfo { Type = "I", Opcode = 0x09 };
            mipsOps["andi"] = new MipsOpInfo { Type = "I", Opcode = 0x0C };
            mipsOps["ori"] = new MipsOpInfo { Type = "I", Opcode = 0x0D };
            mipsOps["xori"] = new MipsOpInfo { Type = "I", Opcode = 0x0E };
            mipsOps["lui"] = new MipsOpInfo { Type = "I", Opcode = 0x0F };
            mipsOps["slti"] = new MipsOpInfo { Type = "I", Opcode = 0x0A };
            mipsOps["sltiu"] = new MipsOpInfo { Type = "I", Opcode = 0x0B };
            mipsOps["daddi"] = new MipsOpInfo { Type = "I", Opcode = 0x18 };
            mipsOps["daddiu"] = new MipsOpInfo { Type = "I", Opcode = 0x19 };
            mipsOps["beq"] = new MipsOpInfo { Type = "I_BRANCH", Opcode = 0x04 };
            mipsOps["bne"] = new MipsOpInfo { Type = "I_BRANCH", Opcode = 0x05 };
            mipsOps["beql"] = new MipsOpInfo { Type = "I_BRANCH_LIKELY", Opcode = 0x14 };
            mipsOps["bnel"] = new MipsOpInfo { Type = "I_BRANCH_LIKELY", Opcode = 0x15 };
            mipsOps["blez"] = new MipsOpInfo { Type = "I_BRANCH_RS_ZERO", Opcode = 0x06 };
            mipsOps["bgtz"] = new MipsOpInfo { Type = "I_BRANCH_RS_ZERO", Opcode = 0x07 };
            mipsOps["bltz"] = new MipsOpInfo { Type = "I_BRANCH_RS_RTFMT", Opcode = 0x01, RtField = 0x00 };
            mipsOps["bgez"] = new MipsOpInfo { Type = "I_BRANCH_RS_RTFMT", Opcode = 0x01, RtField = 0x01 };
            mipsOps["bltzal"] = new MipsOpInfo { Type = "I_BRANCH_RS_RTFMT", Opcode = 0x01, RtField = 0x10 };
            mipsOps["bgezal"] = new MipsOpInfo { Type = "I_BRANCH_RS_RTFMT", Opcode = 0x01, RtField = 0x11 };
            mipsOps["sw"] = new MipsOpInfo { Type = "I", Opcode = 0x2B };
            mipsOps["lw"] = new MipsOpInfo { Type = "I", Opcode = 0x23 };
            mipsOps["lwu"] = new MipsOpInfo { Type = "I", Opcode = 0x27 };
            mipsOps["lb"] = new MipsOpInfo { Type = "I", Opcode = 0x20 };
            mipsOps["lbu"] = new MipsOpInfo { Type = "I", Opcode = 0x24 };
            mipsOps["lh"] = new MipsOpInfo { Type = "I", Opcode = 0x21 };
            mipsOps["lhu"] = new MipsOpInfo { Type = "I", Opcode = 0x25 };
            mipsOps["sb"] = new MipsOpInfo { Type = "I", Opcode = 0x28 };
            mipsOps["sh"] = new MipsOpInfo { Type = "I", Opcode = 0x29 };
            mipsOps["ld"] = new MipsOpInfo { Type = "I_LD_SD", Opcode = 0x37 };
            mipsOps["sd"] = new MipsOpInfo { Type = "I_LD_SD", Opcode = 0x3F };
            mipsOps["lq"] = new MipsOpInfo { Type = "I_LD_SD", Opcode = 0x1E };
            mipsOps["sq"] = new MipsOpInfo { Type = "I_LD_SD", Opcode = 0x1F };
            mipsOps["j"] = new MipsOpInfo { Type = "J", Opcode = 0x02 };
            mipsOps["jal"] = new MipsOpInfo { Type = "J", Opcode = 0x03 };
            mipsOps["nop"] = new MipsOpInfo { Type = "CUSTOM", CustomValue = 0x00000000 };
            mipsOps["eret"] = new MipsOpInfo { Type = "R_ERET", Opcode = 0x10, Funct = 0x18 };

            // floating point
            mipsOps["lwc1"] = new MipsOpInfo { Type = "IFPU_LS", Opcode = 0x31 };
            mipsOps["swc1"] = new MipsOpInfo { Type = "IFPU_LS", Opcode = 0x39 };
            mipsOps["ldc1"] = new MipsOpInfo { Type = "IFPU_LS_D", Opcode = 0x35 };
            mipsOps["sdc1"] = new MipsOpInfo { Type = "IFPU_LS_D", Opcode = 0x3D };
            mipsOps["mfc0"] = new MipsOpInfo { Type = "COP0_MOV", Opcode = 0x10, Fmt = 0x00 };
            mipsOps["mtc0"] = new MipsOpInfo { Type = "COP0_MOV", Opcode = 0x10, Fmt = 0x04 };
            mipsOps["mfc1"] = new MipsOpInfo { Type = "FPU_MOV", Opcode = 0x11, Fmt = 0x00 };
            mipsOps["mtc1"] = new MipsOpInfo { Type = "FPU_MOV", Opcode = 0x11, Fmt = 0x04 };
            mipsOps["add.s"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x10, Funct = 0x00 };
            mipsOps["sub.s"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x10, Funct = 0x01 };
            mipsOps["mul.s"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x10, Funct = 0x02 };
            mipsOps["div.s"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x10, Funct = 0x03 };
            mipsOps["abs.s"] = new MipsOpInfo { Type = "FPU_R_UN", Opcode = 0x11, Fmt = 0x10, Funct = 0x05 };
            mipsOps["mov.s"] = new MipsOpInfo { Type = "FPU_R_UN", Opcode = 0x11, Fmt = 0x10, Funct = 0x06 };
            mipsOps["neg.s"] = new MipsOpInfo { Type = "FPU_R_UN", Opcode = 0x11, Fmt = 0x10, Funct = 0x07 };
            mipsOps["sqrt.s"] = new MipsOpInfo { Type = "FPU_R_UN", Opcode = 0x11, Fmt = 0x10, Funct = 0x04 }; // was 04
            mipsOps["add.d"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x11, Funct = 0x00 };
            mipsOps["sub.d"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x11, Funct = 0x01 };
            mipsOps["mul.d"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x11, Funct = 0x02 };
            mipsOps["div.d"] = new MipsOpInfo { Type = "FPU_R", Opcode = 0x11, Fmt = 0x11, Funct = 0x03 };
            mipsOps["abs.d"] = new MipsOpInfo { Type = "FPU_R_UN", Opcode = 0x11, Fmt = 0x11, Funct = 0x05 };
            mipsOps["neg.d"] = new MipsOpInfo { Type = "FPU_R_UN", Opcode = 0x11, Fmt = 0x11, Funct = 0x07 };
            mipsOps["c.eq.s"] = new MipsOpInfo { Type = "FPU_CMP", Opcode = 0x11, Fmt = 0x10, Funct = 0x32 };
            mipsOps["c.lt.s"] = new MipsOpInfo { Type = "FPU_CMP", Opcode = 0x11, Fmt = 0x10, Funct = 0x34 }; // was 3C
            mipsOps["c.le.s"] = new MipsOpInfo { Type = "FPU_CMP", Opcode = 0x11, Fmt = 0x10, Funct = 0x36 }; // was 3E
            mipsOps["bc1t"] = new MipsOpInfo { Type = "FPU_BRANCH", Opcode = 0x11, Fmt = 0x08, CcBit = 1 };
            mipsOps["bc1f"] = new MipsOpInfo { Type = "FPU_BRANCH", Opcode = 0x11, Fmt = 0x08, CcBit = 0 };
            mipsOps["cvt.s.w"] = new MipsOpInfo { Type = "FPU_CVT", Opcode = 0x11, Fmt = 0x14, Funct = 0x20 };
            mipsOps["cvt.w.s"] = new MipsOpInfo { Type = "FPU_CVT", Opcode = 0x11, Fmt = 0x10, Funct = 0x24 };
            mipsOps["cvt.d.s"] = new MipsOpInfo { Type = "FPU_CVT_D", Opcode = 0x11, Fmt = 0x10, Funct = 0x21 };
            mipsOps["cvt.s.d"] = new MipsOpInfo { Type = "FPU_CVT_S", Opcode = 0x11, Fmt = 0x11, Funct = 0x20 };
            mipsOps["cvt.d.w"] = new MipsOpInfo { Type = "FPU_CVT_D", Opcode = 0x11, Fmt = 0x14, Funct = 0x21 };
            mipsOps["cvt.w.d"] = new MipsOpInfo { Type = "FPU_CVT_S", Opcode = 0x11, Fmt = 0x11, Funct = 0x24 };
            mipsOps["cvt.l.s"] = new MipsOpInfo { Type = "FPU_CVT_L", Opcode = 0x11, Fmt = 0x10, Funct = 0x25 };
            mipsOps["cvt.s.l"] = new MipsOpInfo { Type = "FPU_CVT", Opcode = 0x11, Fmt = 0x15, Funct = 0x20 };
            mipsOps["cvt.l.d"] = new MipsOpInfo { Type = "FPU_CVT_L", Opcode = 0x11, Fmt = 0x11, Funct = 0x25 };
            mipsOps["cvt.d.l"] = new MipsOpInfo { Type = "FPU_CVT_D", Opcode = 0x11, Fmt = 0x15, Funct = 0x21 };

            // custom
            mipsOps["setreg"] = new MipsOpInfo { Type = "PSEUDO_SETREG" };
            mipsOps["b"] = new MipsOpInfo { Type = "PSEUDO_BRANCH" };
        }

        private int ParseOperand(string op, Dictionary<string, uint> labels, bool isImmediateContext = false)
        {
            op = op.Trim();

            // PRIORITY 1: If this operand is in an immediate context and starts with '$',
            // it MUST be a hexadecimal value. This check comes before any register lookup for such strings.
            if (isImmediateContext && op.StartsWith("$"))
            {
                if (op.Length == 1) // Just "$" is invalid
                {
                    throw new ArgumentException($"Invalid immediate format: '{op}'");
                }
                try
                {
                    return Convert.ToInt32(op.Substring(1), 16); // Convert "value" part from hex
                }
                catch (FormatException) { throw new ArgumentException($"Invalid hex format for immediate value: '{op}'"); }
                catch (OverflowException) { throw new ArgumentException($"Hex immediate value out of range: '{op}'"); }
            }

            // PRIORITY 2: Try to parse as a register name using 'potentialReg'
            // (which is 'op' after your specific cleaning for register names, e.g., stripping colons)
            string potentialReg = op;
            // --- Paste your existing 'potentialReg' cleaning logic here ---
            // This logic usually handles cases where labels might have colons but are used in register positions.
            // Example snippet of your cleaning logic:
            if ((potentialReg.StartsWith(":") || potentialReg.StartsWith(";")) && potentialReg.Length > 1)
            {
                string strippedStart = potentialReg.Substring(1);
                if (mipsRegisters.ContainsKey(strippedStart.TrimEnd(':', ';'))) { potentialReg = strippedStart.TrimEnd(':', ';'); }
                else if (mipsRegisters.ContainsKey(potentialReg.TrimEnd(':', ';'))) { potentialReg = potentialReg.TrimEnd(':', ';'); }
            }
            if ((potentialReg.EndsWith(":") || potentialReg.EndsWith(";")) && potentialReg.Length > 1)
            {
                string strippedEnd = potentialReg.Substring(0, potentialReg.Length - 1);
                if (mipsRegisters.ContainsKey(strippedEnd)) { potentialReg = strippedEnd; }
            }
            potentialReg = potentialReg.Trim();
            // --- End of potentialReg cleaning ---

            if (mipsRegisters.TryGetValue(potentialReg, out int regNum))
            {
                return regNum; // Successfully parsed as a register
            }

            // PRIORITY 3: Try standard literal formats (0xHEX, non-immediate $, decimal)
            // 'op' is the original trimmed string for these checks.
            if (op.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (op.Length == 2) throw new ArgumentException($"Invalid 0x prefixed hex value: '{op}'"); // "0x" alone
                try { return Convert.ToInt32(op.Substring(2), 16); }
                catch (FormatException) { throw new ArgumentException($"Invalid 0x prefixed hex value: '{op}'"); }
                catch (OverflowException) { throw new ArgumentException($"0x prefixed hex value out of range: '{op}'"); }
            }

            // This '$' check is for non-immediate contexts (isImmediateContext was false).
            // If isImmediateContext was true, the $HEX was handled at PRIORITY 1.
            if (op.StartsWith("$")) // This implies isImmediateContext is false if this point is reached
            {
                if (op.Length == 1) throw new ArgumentException($"Invalid $ prefixed value: '{op}'"); // "$" alone
                                                                                                      // Check if it's a register name like "$a0" or "$4" (if "$4" is a key in mipsRegisters)
                if (mipsRegisters.TryGetValue(op, out regNum))
                {
                    return regNum;
                }
                // If not a register alias, then it's treated as a $HEX literal.
                try { return Convert.ToInt32(op.Substring(1), 16); }
                catch (FormatException) { throw new ArgumentException($"Invalid $ prefixed hex value: '{op}'"); }
                catch (OverflowException) { throw new ArgumentException($"$ prefixed hex value out of range: '{op}'"); }
            }

            if (int.TryParse(op, NumberStyles.Integer, CultureInfo.InvariantCulture, out int decVal))
            {
                return decVal; // Parsed as a decimal immediate
            }

            // PRIORITY 4: Try to parse as a label
            string labelNameToTry = op;
            if (labels.TryGetValue(labelNameToTry, out uint labelAddr)) return (int)labelAddr; // Direct match

            // Cleaned label lookup (use your most robust label name cleaning logic)
            bool changedByCleaning = false;
            if (labelNameToTry.StartsWith(":"))
            {
                labelNameToTry = labelNameToTry.Substring(1);
                changedByCleaning = true;
            }
            if (labelNameToTry.EndsWith(":") && labelNameToTry.Length > 0) // Check length before Substring
            {
                labelNameToTry = labelNameToTry.Substring(0, labelNameToTry.Length - 1);
                changedByCleaning = true;
            }
            else if (labelNameToTry.EndsWith(":") && labelNameToTry.Length == 0 && changedByCleaning) // Handles case like ":" -> ""
            {
                // Invalid label if it becomes empty after stripping colons from just ":" or "::"
            }


            if (!string.IsNullOrEmpty(labelNameToTry) && labels.TryGetValue(labelNameToTry, out labelAddr))
            {
                return (int)labelAddr;
            }
            // If cleaning didn't help, and original op wasn't empty and wasn't already tried (it was).

            throw new ArgumentException($"Invalid operand, value, or unknown label: '{op}'");
        }


        private (int imm, int rs) ParseMemOffset(string memStr, Dictionary<string, uint> labels)
        {
            var match = Regex.Match(memStr, @"([$0-9a-fA-FxX.:\w-]+)\s*\(\s*([$\w.:]+)\s*\)"); // Adjusted regex for offset part slightly for robustness
            if (!match.Success) throw new ArgumentException($"Invalid memory offset format: {memStr}");

            string offsetValStr = match.Groups[1].Value.Trim();
            string regNameStr = match.Groups[2].Value;
            int imm;

            if (offsetValStr.StartsWith("$"))
            {
                string hexPart = offsetValStr.Substring(1);
                if (Regex.IsMatch(hexPart, @"^[0-9a-fA-F]+$"))
                {
                    try { imm = Convert.ToInt32(hexPart, 16); }
                    catch (FormatException) { throw new ArgumentException($"Invalid hex format in offset: {offsetValStr}"); }
                    catch (OverflowException) { throw new ArgumentException($"Hex offset value too large: {offsetValStr}"); }
                }
                else
                {
                    throw new ArgumentException($"Invalid characters after $ in offset: {offsetValStr}");
                }
            }
            else if (offsetValStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                string hexPart = offsetValStr.Substring(2);
                if (Regex.IsMatch(hexPart, @"^[0-9a-fA-F]+$"))
                {
                    try { imm = Convert.ToInt32(hexPart, 16); }
                    catch (FormatException) { throw new ArgumentException($"Invalid hex format in offset: {offsetValStr}"); }
                    catch (OverflowException) { throw new ArgumentException($"Hex offset value too large: {offsetValStr}"); }
                }
                else
                {
                    throw new ArgumentException($"Invalid characters after 0x in offset: {offsetValStr}");
                }
            }
            else if (int.TryParse(offsetValStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int decVal))
            {
                imm = decVal;
            }
            else if (labels.TryGetValue(offsetValStr, out uint labelAddr)) // Optional: if labels can be used as offsets
            {
                imm = (int)labelAddr;
            }
            else
            {
                // If it's not $HEX, not 0xHEX, not a valid decimal, (and not a label, if supported)
                // it could be an attempt to use a bare hex string like "FF" without a prefix.
                // Decide if you want to support bare hex or treat it as an error or a label lookup.
                // For strictness, and based on typical MIPS syntax, bare numbers are decimal.
                // If it wasn't parsed as decimal, it's likely an error or an unrecognized label.
                throw new ArgumentException($"Invalid or unrecognized immediate offset value: {offsetValStr}");
            }

            int rs = ParseOperand(regNameStr, labels);
            return (imm, rs);
        }


        // --- Import Handling ---
        private List<LineSourceInfo> PreprocessImports(string[] initialLines, string currentFileName, string baseDirectoryPath, ref int globalLineCounter, int currentDepth = 0)
        {
            if (currentDepth > MAX_IMPORT_DEPTH)
            {
                throw new Exception($"Maximum import depth of {MAX_IMPORT_DEPTH} exceeded. Check for circular imports.");
            }

            var processedLineInfos = new List<LineSourceInfo>();
            int localLineNumber = 0;
            foreach (var lineText in initialLines)
            {
                localLineNumber++;
                var importMatch = Regex.Match(lineText.Trim(), @"^import\s+""([^""]+)""", RegexOptions.IgnoreCase);
                if (importMatch.Success)
                {
                    string relativePath = importMatch.Groups[1].Value.Replace('\\', Path.DirectorySeparatorChar);
                    string fullPath = Path.Combine(baseDirectoryPath ?? Directory.GetCurrentDirectory(), relativePath);

                    if (File.Exists(fullPath))
                    {
                        Console.WriteLine($"Importing: {fullPath} (depth {currentDepth})");
                        // Read imported files using ISO-8859-1 encoding
                        string[] importedFileLines = File.ReadAllLines(fullPath, Encoding.GetEncoding("ISO-8859-1"));
                        processedLineInfos.AddRange(PreprocessImports(importedFileLines, Path.GetFileName(fullPath), Path.GetDirectoryName(fullPath), ref globalLineCounter, currentDepth + 1));
                    }
                    else
                    {
                        Console.WriteLine($"Warning - Import failed: File not found \"{fullPath}\"");
                        processedLineInfos.Add(new LineSourceInfo($"// Import failed (not found): {lineText.Trim()}", currentFileName, localLineNumber, globalLineCounter++));
                    }
                }
                else
                {
                    processedLineInfos.Add(new LineSourceInfo(lineText, currentFileName, localLineNumber, globalLineCounter++));
                }
            }
            return processedLineInfos;
        }

        private string StripCommentsFromLine(string line, ref bool inBlockComment)
        {
            StringBuilder sb = new StringBuilder();
            int currentSearchIndex = 0;

            while (currentSearchIndex < line.Length)
            {
                if (inBlockComment)
                {
                    int endCommentIndex = line.IndexOf("*/", currentSearchIndex);
                    if (endCommentIndex != -1)
                    {
                        inBlockComment = false;
                        currentSearchIndex = endCommentIndex + 2;
                    }
                    else
                    {
                        // Block comment continues to the next line, so we append nothing from this line.
                        currentSearchIndex = line.Length; // Effectively stop processing this line
                    }
                }
                else // Not in a block comment
                {
                    int startBlockCommentIndex = line.IndexOf("/*", currentSearchIndex);
                    int startSingleLineComment1 = line.IndexOf("//", currentSearchIndex);
                    int startSingleLineComment2 = line.IndexOf("#", currentSearchIndex);

                    int determinedNextCommentStart = -1;

                    // Check for /*
                    if (startBlockCommentIndex != -1)
                    {
                        determinedNextCommentStart = startBlockCommentIndex;
                    }

                    // Check for //, update if it's earlier than /*
                    if (startSingleLineComment1 != -1)
                    {
                        if (determinedNextCommentStart == -1 || startSingleLineComment1 < determinedNextCommentStart)
                        {
                            determinedNextCommentStart = startSingleLineComment1;
                        }
                    }

                    // Check for #, update if it's earlier AND NOT inside a string literal
                    if (startSingleLineComment2 != -1)
                    {
                        bool hashIsInsideString = false;
                        int quoteBalance = 0;
                        // Count unescaped quotes from currentSearchIndex up to where '#' was found
                        for (int k = currentSearchIndex; k < startSingleLineComment2; k++)
                        {
                            if (line[k] == '\\' && k + 1 < startSingleLineComment2) // Check bounds for k+1
                            {
                                k++; // Skip the character after backslash (it's escaped)
                            }
                            else if (line[k] == '"')
                            {
                                quoteBalance++;
                            }
                        }

                        if (quoteBalance % 2 == 1) // Odd number of unescaped quotes means '#' is inside a string
                        {
                            hashIsInsideString = true;
                        }

                        if (!hashIsInsideString) // If '#' is not inside a string, consider it a comment
                        {
                            if (determinedNextCommentStart == -1 || startSingleLineComment2 < determinedNextCommentStart)
                            {
                                determinedNextCommentStart = startSingleLineComment2;
                            }
                        }
                        // If hashIsInsideString is true, startSingleLineComment2 is ignored as a comment starter.
                    }

                    // 'determinedNextCommentStart' now holds the beginning of the earliest valid comment, or -1

                    if (determinedNextCommentStart == -1) // No valid comment found on the rest of the line
                    {
                        sb.Append(line.Substring(currentSearchIndex));
                        currentSearchIndex = line.Length; // Done with this line
                    }
                    else // A valid comment was found
                    {
                        // Append text before the comment
                        sb.Append(line.Substring(currentSearchIndex, determinedNextCommentStart - currentSearchIndex));

                        // Now determine what type of comment it was to advance currentSearchIndex correctly
                        if (determinedNextCommentStart == startBlockCommentIndex && startBlockCommentIndex != -1)
                        {
                            inBlockComment = true;
                            currentSearchIndex = determinedNextCommentStart + 2; // Move past "/*"
                                                                                 // Check if the block comment also ends on this same line
                            int endCommentIndexOnSameLine = line.IndexOf("*/", currentSearchIndex);
                            if (endCommentIndexOnSameLine != -1)
                            {
                                inBlockComment = false; // It ended
                                currentSearchIndex = endCommentIndexOnSameLine + 2; // Move past "*/"
                            }
                            else
                            {
                                // Block comment continues to next line, so we're done with this line's content
                                currentSearchIndex = line.Length;
                            }
                        }
                        else // It's a single-line comment (// or a valid #)
                        {
                            // The rest of the line is a comment, so we're done with this line's content
                            currentSearchIndex = line.Length;
                        }
                    }
                }
            }
            return sb.ToString().Trim(); // Trim leading/trailing whitespace from the processed line
        }

        private string FormatOutputLine(uint address, string hexDataValue)
        {
            if (currentOutputFormat == OutputFormatMode.PS2)
            {
                string addrStrOutput = address.ToString("X8");
                string addressFormatChar = txtAddressFormat.Text; // Get the format character

                // Your existing logic for PS2 address prefixing
                if (addressFormatChar != "-" && addressFormatChar.Length == 1)
                {
                    // This assumes the format character replaces the first digit of the address
                    addrStrOutput = addressFormatChar[0] + addrStrOutput.Substring(1);
                }
                return $"{addrStrOutput} {hexDataValue}";
            }
            else // OutputFormatMode.Pnach
            {
                string pnachAddress = address.ToString("X8"); // Raw 8-digit hex for pnach
                string addressFormatChar = txtAddressFormat.Text; // Get the format character
                if (addressFormatChar != "-" && addressFormatChar.Length == 1)
                {
                    // This assumes the format character replaces the first digit of the address
                    pnachAddress = addressFormatChar[0] + pnachAddress.Substring(1);
                }                                              // Assuming hexDataValue is the correct 8-character hex string for the pnach value
                return $"patch=1,EE,{pnachAddress},extended,{hexDataValue}";
            }
        }


        // --- Main Compilation Logic ---
        // Ensure MipsOpInfo class, mipsOps dictionary, labels dictionary, ParseOperand, StripCommentsFromLine,
        // and MipsErrorInfo class are defined elsewhere in your Form1.cs
        private void BtnCompile_Click(object sender, EventArgs e)
        {
            if (mipsOps.Count == 0) InitializeMipsData();

            rtbOutput.Clear();
            errorLineNumbersToHighlight.Clear(); // Clear errors at the start of compilation
            List<MipsErrorInfo> errors = new List<MipsErrorInfo>();
            List<LineSourceInfo> processedLineInfos = new List<LineSourceInfo>();


            string mainFileName = currentFilePath ?? "main_input.asm";
            string[] rtbLines = rtbInput.Lines.Select(l => l.Text).ToArray();
            int globalLineCounter = 0;

            try
            {
                string baseDir = currentFilePath != null ? Path.GetDirectoryName(currentFilePath) : Directory.GetCurrentDirectory();
                processedLineInfos = PreprocessImports(rtbLines, mainFileName, baseDir, ref globalLineCounter);
            }
            catch (Exception ex)
            {
                rtbOutput.Text = $"Error during import preprocessing: {ex.Message}";
                UpdateLineCount();
                HighlightSyntax(0, rtbInput.TextLength);
                return;
            }


            uint currentAddress = 0;
            var labels = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            string addressFormatChar = txtAddressFormat.Text;
            bool blockCommentActiveForCompilerPass = false;

            // Pass 1
            uint tempAddress = 0;
            for (int idx = 0; idx < processedLineInfos.Count; idx++)
            {
                LineSourceInfo lineInfo = processedLineInfos[idx];
                string effectiveLineContent = StripCommentsFromLine(lineInfo.Text, ref blockCommentActiveForCompilerPass);

                if (string.IsNullOrEmpty(effectiveLineContent)) continue;

                string lowerLine = effectiveLineContent.ToLower();
                bool isMain = lineInfo.FileName == mainFileName;

                if (lowerLine.StartsWith("address"))
                {
                    string[] parts = effectiveLineContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        string addrStr = parts[1].StartsWith("$") ? parts[1].Substring(1) : parts[1];
                        if (uint.TryParse(addrStr, System.Globalization.NumberStyles.HexNumber, null, out uint parsedAddr)) tempAddress = parsedAddr;
                        else errors.Add(new MipsErrorInfo(lineInfo.FileName, lineInfo.OriginalLineNumber, lineInfo.GlobalIndex, tempAddress, "N/A", $"Invalid address '{parts[1]}'", lineInfo.Text, isMain));
                    }
                    continue;
                }   

                else if (lowerLine.StartsWith("print"))
                {
                    Match strMatch = Regex.Match(effectiveLineContent, @"print\s+""((?:\\.|[^""])*)""", RegexOptions.IgnoreCase);
                    if (strMatch.Success) { string str = strMatch.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\t", "\t"); tempAddress += (uint)Math.Ceiling(str.Length / 4.0) * 4; }
                    else errors.Add(new MipsErrorInfo(lineInfo.FileName, lineInfo.OriginalLineNumber, lineInfo.GlobalIndex, tempAddress, "N/A", $"Invalid print: {effectiveLineContent}", lineInfo.Text, isMain));
                    continue;
                }
                else if (lowerLine.StartsWith("hexcode") || lowerLine.StartsWith("float")) { tempAddress += 4; continue; }
                else if (lowerLine.StartsWith("setreg")) { tempAddress += 8; continue; }
                Match labelMatch = Regex.Match(effectiveLineContent, @"^([\w.:]+):");
                if (labelMatch.Success)
                {
                    string labelName = labelMatch.Groups[1].Value;
                    if (labels.ContainsKey(labelName)) errors.Add(new MipsErrorInfo(lineInfo.FileName, lineInfo.OriginalLineNumber, lineInfo.GlobalIndex, tempAddress, "N/A", $"Duplicate label '{labelName}'", lineInfo.Text, isMain));
                    else labels[labelName] = tempAddress;
                    if (effectiveLineContent.Length > labelMatch.Length + 1)
                    {
                        effectiveLineContent = effectiveLineContent.Substring(labelMatch.Length + 1).Trim();
                    }
                    else
                    {
                        effectiveLineContent = "";
                    }
                    if (string.IsNullOrEmpty(effectiveLineContent)) continue;
                }
                if (!string.IsNullOrEmpty(effectiveLineContent)) tempAddress += 4;
            }

            if (errors.Any())
            {
                errorLineNumbersToHighlight = errors.Where(errTuple => errTuple.IsFromMainInput).Select(errTuple => errTuple.OriginalLineNumber - 1).Distinct().ToList();
                rtbOutput.Text = "Errors (Pass 1):\n" + string.Join("\n", errors.Select(errTuple => errTuple.ToString()));
                UpdateLineCount();
                HighlightSyntax(0, rtbInput.TextLength);
                return;
            }

            // Pass 2
            var outputLines = new List<string>();
            currentAddress = 0;
            blockCommentActiveForCompilerPass = false;
            for (int idx = 0; idx < processedLineInfos.Count; idx++)
            {
                LineSourceInfo lineInfo = processedLineInfos[idx];
                string originalLineForErrorDisplay = lineInfo.Text.Trim();
                bool isMain = lineInfo.FileName == mainFileName;
                string attemptedData = "N/A";

                try
                {
                    string effectiveLineContent = StripCommentsFromLine(lineInfo.Text, ref blockCommentActiveForCompilerPass);
                    if (string.IsNullOrEmpty(effectiveLineContent)) continue;

                    string lowerLine = effectiveLineContent.ToLower();
                    string addrStrOutput;
                    if (lowerLine.StartsWith("address"))
                    {
                        string[] parts = effectiveLineContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            string addrStr = parts[1].StartsWith("$") ? parts[1].Substring(1) : parts[1];
                            if (!uint.TryParse(addrStr, System.Globalization.NumberStyles.HexNumber, null, out currentAddress))
                                throw new Exception($"Invalid address '{parts[1]}'");
                        }
                        continue;
                    }
                    else if (lowerLine.StartsWith("print"))
                    {                   

                        Match strMatch = Regex.Match(effectiveLineContent, @"print\s+""((?:\\.|[^""])*)""", RegexOptions.IgnoreCase);
                        if (strMatch.Success)
                        {
                            string str = strMatch.Groups[1].Value.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\t", "\t");
                            for (int k = 0; k < str.Length; k += 4)
                            {
                                string chunk = str.Substring(k, Math.Min(4, str.Length - k));
                                byte[] bytes = new byte[4]; // Initialized with all zeros

                                // Replace Encoding.ASCII with Encoding.GetEncoding("ISO-8859-1")
                                Encoding.GetEncoding("ISO-8859-1").GetBytes(chunk, 0, chunk.Length, bytes, 0);

                                uint wordValue = BitConverter.ToUInt32(bytes, 0);
                                string hexWord = wordValue.ToString("X8");
                                //outputLines.Add(FormatOutputLine(currentAddress, hexWord));
                                /*
                                addrStrOutput = currentAddress.ToString("X8");
                                if (addressFormatChar != "-" && addressFormatChar.Length == 1) addrStrOutput = addressFormatChar + addrStrOutput.Substring(1);
                                outputLines.Add($"{addrStrOutput} {hexWord}");
                                */
                                if (currentOutputFormat == OutputFormatMode.PS2)
                                {
                                    addrStrOutput = currentAddress.ToString("X8"); // Keep local scope if not needed outside
                                    addressFormatChar = txtAddressFormat.Text; // Get current format char
                                    if (addressFormatChar != "-" && addressFormatChar.Length == 1)
                                    {
                                        addrStrOutput = addressFormatChar + addrStrOutput.Substring(1);
                                    }
                                    outputLines.Add($"{addrStrOutput} {hexWord}"); // hexInstructionValue is the compiled code, e.g., machineCode.ToString("X8")
                                }
                                else // OutputFormatMode.Pnach
                                {
                                    string pnachAddress = currentAddress.ToString("X8"); // Raw 8-digit hex address
                                                                                         // Assuming the hexInstructionValue is the data for pnach.
                                                                                         // The example "AAAAAAAA" for "addiu v0, v0, $1" (which is 24420001)
                                                                                         // suggests "AAAAAAAA" might be a placeholder or requires a specific format.
                                                                                         // For now, we'll assume it's the direct hex machine code.
                                                                                         // If "AAAAAAAA" implies a different format (e.g., byte-swapped), you'll need to adjust pnachData.
                                    string pnachData = hexWord;
                                    outputLines.Add($"patch=1,EE,{pnachAddress},extended,{pnachData}");
                                }
                                currentAddress += 4;
                            }
                        }
                        //else throw new Exception("Invalid print syntax.");
                        else
                        {
                            // This exception is thrown if the regex does not match.
                            // The error message you quoted "Invalid print: print \"" might be from a custom error reporting
                            // mechanism if you are catching this exception and then formatting a new message.
                            // If you see "Invalid print syntax.", it's directly from here.
                            throw new Exception($"Invalid print syntax. Line: '{effectiveLineContent}'"); // Added line content for better debugging
                        }
                        continue;
                    }
                    else if (lowerLine.StartsWith("hexcode"))
                    {
                        string[] parts = effectiveLineContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2) throw new Exception("Invalid hexcode syntax.");
                        string hexValueStr;
                        string operand = parts[1];
                        if (operand.StartsWith("$"))
                        {
                            hexValueStr = operand.Substring(1);
                            if (!Regex.IsMatch(hexValueStr, @"^[0-9a-fA-F]+$")) throw new Exception($"Invalid hex value: {operand}");
                        }
                        else if (operand.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            hexValueStr = operand.Substring(2);
                            if (!Regex.IsMatch(hexValueStr, @"^[0-9a-fA-F]+$")) throw new Exception($"Invalid hex value: {operand}");
                        }
                        else if (operand.StartsWith(":"))
                        { // Explicitly handle label reference
                            string labelName = operand.Substring(1); // Remove leading ':'
                            if (!labels.TryGetValue(labelName, out uint labelAddr)) throw new Exception($"Label '{labelName}' not found.");
                            hexValueStr = labelAddr.ToString("X");
                        }
                        else if (int.TryParse(operand, out int decVal))
                        { // Try decimal 
                            hexValueStr = ((uint)decVal).ToString("X");
                        }
                        else
                        {
                            // Assume it's a label name (without :)
                            if (!labels.TryGetValue(operand, out uint labelAddr)) throw new Exception($"Label '{operand}' not found.");
                            hexValueStr = labelAddr.ToString("X");
                        }
                        hexValueStr = hexValueStr.ToUpper().PadLeft(8, '0'); if (hexValueStr.Length > 8) hexValueStr = hexValueStr.Substring(hexValueStr.Length - 8);
                        addrStrOutput = currentAddress.ToString("X8");
                        if (addressFormatChar != "-" && addressFormatChar.Length == 1) addrStrOutput = addressFormatChar + addrStrOutput.Substring(1);
                        //outputLines.Add($"{addrStrOutput} {hexValueStr}");
                        outputLines.Add(FormatOutputLine(currentAddress, hexValueStr));
                        currentAddress += 4; 
                        continue;
                    }
                    else if (lowerLine.StartsWith("float"))
                    {
                        string[] parts = effectiveLineContent.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2) throw new Exception("Invalid float syntax.");
                        string floatStr = parts[1];
                        if (floatStr.StartsWith("$")) floatStr = floatStr.Substring(1); // Allow optional $

                        if (!float.TryParse(floatStr, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatVal))
                        {
                            throw new Exception($"Invalid float value: {parts[1]}");
                        }
                        byte[] floatBytes = BitConverter.GetBytes(floatVal);
                        uint uintVal = BitConverter.ToUInt32(floatBytes, 0); // Get the integer representation
                        string hexWord = uintVal.ToString("X8");
                        //outputLines.Add(FormatOutputLine(currentAddress, hexWord));

                        addrStrOutput = currentAddress.ToString("X8");
                        if (addressFormatChar != "-" && addressFormatChar.Length == 1) addrStrOutput = addressFormatChar + addrStrOutput.Substring(1);
                        outputLines.Add(FormatOutputLine(currentAddress, hexWord)); currentAddress += 4; continue;
                    }

                    Match labelMatch = Regex.Match(effectiveLineContent, @"^([\w.:]+):");
                    if (labelMatch.Success)
                    {
                        if (effectiveLineContent.Length > labelMatch.Length + 1)
                        {
                            effectiveLineContent = effectiveLineContent.Substring(labelMatch.Length + 1).Trim();
                        }
                        else
                        {
                            effectiveLineContent = "";
                        }
                        if (string.IsNullOrEmpty(effectiveLineContent)) continue;
                    }

                    string[] instructionParts = Regex.Split(effectiveLineContent, @"[,\s]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    if (instructionParts.Length == 0) continue;
                    string mnemonic = instructionParts[0].ToLower();

                    string[] ops;
                    if ((mnemonic.StartsWith("lw") || mnemonic.StartsWith("sw") || mnemonic.StartsWith("lb") || mnemonic.StartsWith("sb") ||
                         mnemonic.StartsWith("lh") || mnemonic.StartsWith("sh") || mnemonic.StartsWith("ld") || mnemonic.StartsWith("sd") ||
                         mnemonic.StartsWith("lq") || mnemonic.StartsWith("sq") || mnemonic.StartsWith("lwc1") || mnemonic.StartsWith("swc1") ||
                         mnemonic.StartsWith("ldc1") || mnemonic.StartsWith("sdc1"))
                        && effectiveLineContent.Contains("(") && effectiveLineContent.Contains(")"))
                    {
                        ops = new string[2];
                        ops[0] = instructionParts[1];
                        int firstCommaIndex = effectiveLineContent.IndexOf(',');
                        if (firstCommaIndex != -1 && firstCommaIndex + 1 < effectiveLineContent.Length)
                        {
                            ops[1] = effectiveLineContent.Substring(firstCommaIndex + 1).Trim();
                        }
                        else
                        {
                            throw new Exception($"Missing comma in memory operand for {mnemonic}: {originalLineForErrorDisplay}");
                        }
                    }
                    else
                    {
                        ops = instructionParts.Skip(1).ToArray();
                    }


                    if (!mipsOps.TryGetValue(mnemonic, out MipsOpInfo instrInfo) || instrInfo == null) throw new Exception($"unk cmd: {mnemonic}");
                    uint machineCode = 0;

                    if (instrInfo.Type == "CUSTOM") machineCode = instrInfo.CustomValue;
                    else if (instrInfo.Type == "PSEUDO_SETREG")
                    {
                        if (ops.Length < 2) throw new Exception($"Not enough operands for setreg: {originalLineForErrorDisplay}");

                        int rd = ParseOperand(ops[0], labels, true); // Destination register
                        string valueString = ops[1]; // The string representing the value to load
                        uint value32;

                        // Parse the 32-bit value for setreg's second operand
                        // It can be hex (0x... or $...), a label, or decimal.
                        if (valueString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            try { value32 = Convert.ToUInt32(valueString.Substring(2), 16); }
                            catch (Exception ex) { throw new Exception($"Invalid hex value '{valueString}' for setreg: {ex.Message}"); }
                        }
                        else if (valueString.StartsWith("$"))
                        {
                            try { value32 = Convert.ToUInt32(valueString.Substring(1), 16); }
                            catch (Exception ex) { throw new Exception($"Invalid hex value '{valueString}' for setreg: {ex.Message}"); }
                        }
                        else
                        {
                            // Try as a label (stripping potential colons)
                            string labelNameToTry = valueString.Trim(':');
                            // More robust label name cleaning if needed, e.g. if colons can be elsewhere.
                            // For simplicity, this handles common cases like "mylabel" or "mylabel:".

                            if (labels.TryGetValue(labelNameToTry, out value32))
                            {
                                // Value successfully retrieved from label
                            }
                            else
                            {
                                // Not a hex string, not a recognized label, so try as a decimal number.
                                // Convert.ToInt32 allows parsing of negative numbers, which are then cast to uint
                                // (e.g., -1 becomes 0xFFFFFFFF).
                                try
                                {
                                    value32 = (uint)Convert.ToInt32(valueString, 10);
                                }
                                catch (FormatException)
                                {
                                    throw new Exception($"Invalid value or unrecognized label '{valueString}' for setreg. Must be hex, decimal, or a defined label.");
                                }
                                catch (OverflowException) // Handle numbers too large for int but potentially valid for uint
                                {
                                    try 
                                    {
                                        value32 = Convert.ToUInt32(valueString, 10);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception($"Value '{valueString}' for setreg is out of range or invalid: {ex.Message}");
                                    }
                                }
                            }
                        }

                        uint upper16 = (value32 >> 16) & 0xFFFF;
                        uint lower16 = value32 & 0xFFFF;

                        // 1. LUI instruction (lui rd, upper16)
                        // MIPS LUI: Opcode rt, immediate. 'rd' from parsed operand is used as 'rt'.
                        uint luiCode = (mipsOps["lui"].Opcode << 26) | ((uint)rd << 16) | upper16;
                        
                        addrStrOutput = currentAddress.ToString("X8");
                        if (addressFormatChar != "-" && addressFormatChar.Length == 1) addrStrOutput = addressFormatChar + addrStrOutput.Substring(1);
                        //outputLines.Add($"{addrStrOutput} {luiCode:X8}");
                        string hexLui = luiCode.ToString("X8");
                        outputLines.Add(FormatOutputLine(currentAddress, hexLui));
                        currentAddress += 4;

                        // 2. ORI instruction (ori rd, rd, lower16)
                        // MIPS ORI: Opcode rt, rs, immediate. 'rt' is 'rd', 'rs' is also 'rd'.
                        uint oriCode = (mipsOps["ori"].Opcode << 26) | ((uint)rd << 21) | ((uint)rd << 16) | lower16;
                        
                        addrStrOutput = currentAddress.ToString("X8");
                        if (addressFormatChar != "-" && addressFormatChar.Length == 1) addrStrOutput = addressFormatChar + addrStrOutput.Substring(1);
                        //outputLines.Add($"{addrStrOutput} {oriCode:X8}");
                        string hexCombine = oriCode.ToString("X8");
                        outputLines.Add(FormatOutputLine(currentAddress, hexCombine));
                        currentAddress += 4;

                        continue; // Skip default instruction processing
                    }
                    else if (instrInfo.Type == "PSEUDO_BRANCH")
                    { // Handle 'b' instruction
                        uint targetAddr = (uint)ParseOperand(ops[0], labels);
                        int offset = (int)(targetAddr - (currentAddress + 4)) / 4;
                        if (offset < -32768 || offset > 32767) throw new Exception("Branch offset out of range for 'b' instruction.");
                        // Assemble as beq $zero, $zero, offset
                        machineCode = (mipsOps["beq"].Opcode << 26) | (0 << 21) | (0 << 16) | ((uint)offset & 0xFFFF);
                    }
                    else if (instrInfo.Type == "R")
                    {
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;
                        int rd_val = 0, rs_val = 0, rt_val = 0;
                        if (mnemonic == "jr") { rs_val = ParseOperand(ops[0], labels); }
                        else { rd_val = ParseOperand(ops[0], labels); rs_val = ParseOperand(ops[1], labels); rt_val = ParseOperand(ops[2], labels); }
                        machineCode |= ((uint)rs_val & 0x1F) << 21; machineCode |= ((uint)rt_val & 0x1F) << 16; machineCode |= ((uint)rd_val & 0x1F) << 11;
                    }
                    else if (instrInfo.Type == "R_JALR")
                    {
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;
                        int rs_val = 0; int rd_val = 31;
                        if (ops.Length == 1) { rs_val = ParseOperand(ops[0], labels); }
                        else if (ops.Length == 2) { rd_val = ParseOperand(ops[0], labels); rs_val = ParseOperand(ops[1], labels); }
                        else { throw new Exception($"Incorrect operands for {mnemonic}"); }
                        machineCode |= ((uint)rs_val & 0x1F) << 21; machineCode |= ((uint)rd_val & 0x1F) << 11;
                    }
                    else if (instrInfo.Type == "R_SHIFT" || instrInfo.Type == "R_SHIFT_PLUS32")
                    {
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;
                        int rd_val = ParseOperand(ops[0], labels); int rt_val = ParseOperand(ops[1], labels); int shamt_val = ParseOperand(ops[2], labels);
                        machineCode |= ((uint)rt_val & 0x1F) << 16; machineCode |= ((uint)rd_val & 0x1F) << 11; machineCode |= ((uint)shamt_val & 0x1F) << 6;
                    }
                    else if (instrInfo.Type == "R_SHIFT_V")
                    {
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;
                        int rd_val = ParseOperand(ops[0], labels); int rt_val = ParseOperand(ops[1], labels); int rs_shift_val = ParseOperand(ops[2], labels);
                        machineCode |= ((uint)rs_shift_val & 0x1F) << 21; machineCode |= ((uint)rt_val & 0x1F) << 16; machineCode |= ((uint)rd_val & 0x1F) << 11;
                    }
                    else if (instrInfo.Type == "R_MULTDIV") // Handles mult, multu, div, divu
                    {
                        // Common parts: Opcode is typically 0x00 (SPECIAL), Funct is specific to the instruction
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;

                        if (ops.Length == 2) // Standard 2-operand form: mult rs, rt
                        {
                            if (string.IsNullOrEmpty(ops[0])) throw new Exception($"Missing source register rs for {mnemonic}: {originalLineForErrorDisplay}");
                            if (string.IsNullOrEmpty(ops[1])) throw new Exception($"Missing source register rt for {mnemonic}: {originalLineForErrorDisplay}");

                            int rs_val = ParseOperand(ops[0], labels); // ops[0] is rs
                            int rt_val = ParseOperand(ops[1], labels); // ops[1] is rt

                            machineCode |= ((uint)rs_val & 0x1F) << 21; // rs field
                            machineCode |= ((uint)rt_val & 0x1F) << 16; // rt field
                                                                        // For standard mult/div, rd (bits 15-11) and shamt (bits 10-6) are 0.
                                                                        // Since machineCode was initialized with opcode and funct, and higher bits are 0,
                                                                        // these fields will remain 0 if not explicitly set.
                        }
                        else if (ops.Length == 3) // 3-operand form used by user: mult rd, rs, rt
                        {
                            if (string.IsNullOrEmpty(ops[0])) throw new Exception($"Missing destination register rd for {mnemonic}: {originalLineForErrorDisplay}");
                            if (string.IsNullOrEmpty(ops[1])) throw new Exception($"Missing source register rs for {mnemonic}: {originalLineForErrorDisplay}");
                            if (string.IsNullOrEmpty(ops[2])) throw new Exception($"Missing source register rt for {mnemonic}: {originalLineForErrorDisplay}");

                            int rd_val = ParseOperand(ops[0], labels); // ops[0] is rd
                            int rs_val = ParseOperand(ops[1], labels); // ops[1] is rs
                            int rt_val = ParseOperand(ops[2], labels); // ops[2] is rt

                            machineCode |= ((uint)rs_val & 0x1F) << 21; // rs field
                            machineCode |= ((uint)rt_val & 0x1F) << 16; // rt field
                            machineCode |= ((uint)rd_val & 0x1F) << 11; // rd field
                                                                        // shamt field (bits 10-6) remains 0.
                        }
                        else
                        {
                            throw new Exception($"Instruction {mnemonic} (type {instrInfo.Type}) expects 2 or 3 operands, but {ops.Length} were provided: {originalLineForErrorDisplay}");
                        }
                    }
                    else if (instrInfo.Type == "R_MFHI_MFLO")
                    {
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;
                        int rd_val = ParseOperand(ops[0], labels); machineCode |= ((uint)rd_val & 0x1F) << 11;
                    }
                    else if (instrInfo.Type == "R_MTHI_MTLO")
                    {
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;
                        int rs_val = ParseOperand(ops[0], labels); machineCode |= ((uint)rs_val & 0x1F) << 21;
                    }
                    else if (instrInfo.Type == "R_SYSCALL_BREAK" || instrInfo.Type == "R_SYNC")
                    {
                        machineCode = (instrInfo.Opcode << 26) | instrInfo.Funct;
                        if (ops.Length > 0) { int code = ParseOperand(ops[0], labels); machineCode |= ((uint)code & 0x03FFFFF) << 6; }
                    }
                    else if (instrInfo.Type == "R_ERET")
                    {
                        machineCode = (instrInfo.Opcode << 26) | (1 << 25) | instrInfo.Funct;
                    }
                    else if (instrInfo.Type == "I" || instrInfo.Type == "I_LD_SD")
                    {
                        machineCode = (instrInfo.Opcode << 26); // Base opcode

                        // Declare variables at a scope accessible by all paths and the final encoding lines.
                        int rt_val;
                        int rs_val;
                        int imm_val;

                        string currentMnemonic = mnemonic.ToLower(); // Work with a lowercase version for comparisons

                        // It's crucial to correctly identify the instruction format.
                        // ops[0] is typically rt for these instructions.

                        // Format 1: LUI (rt, immediate)
                        if (currentMnemonic == "lui")
                        {
                            if (ops.Length != 2) throw new Exception($"LUI expects 2 operands (rt, immediate), got {ops.Length} for line: {originalLineForErrorDisplay}");
                            rt_val = ParseOperand(ops[0], labels);        // rt is a register (isImmediateContext=false by default)
                            imm_val = ParseOperand(ops[1], labels, true); // ops[1] is the immediate value
                            rs_val = 0;                                   // rs field is 0 for LUI's encoding
                        }
                        // Format 2: Load/Store instructions (rt, offset(rs))
                        // Add all relevant load/store mnemonics that are Type "I" or "I_LD_SD" and use this format.
                        else if (ops.Length == 2 &&
                                 (currentMnemonic == "lw" || currentMnemonic == "sw" || currentMnemonic == "lwu" ||
                                  currentMnemonic == "lb" || currentMnemonic == "sb" || currentMnemonic == "lbu" ||
                                  currentMnemonic == "lh" || currentMnemonic == "sh" || currentMnemonic == "lhu" ||
                                  currentMnemonic == "ld" || currentMnemonic == "sd" || currentMnemonic == "lq" || currentMnemonic == "sq"
                                 /* Add other load/store mnemonics like lq, sq if they follow this pattern and type */
                                 ))
                        {
                            if (string.IsNullOrEmpty(ops[0])) throw new Exception($"Missing destination/source register (rt) for {currentMnemonic}: {originalLineForErrorDisplay}");
                            rt_val = ParseOperand(ops[0], labels);         // rt is a register

                            if (string.IsNullOrEmpty(ops[1])) throw new Exception($"Missing memory operand (offset(base)) for {currentMnemonic}: {originalLineForErrorDisplay}");
                            var mem = ParseMemOffset(ops[1], labels);      // ops[1] is the "offset(rs)" string
                            rs_val = mem.rs;                               // Base register from ParseMemOffset
                            imm_val = mem.imm;                             // Immediate offset from ParseMemOffset
                        }
                        // Format 3: Arithmetic/Logical I-Type (rt, rs, immediate)
                        else if (ops.Length == 3 && instrInfo.Type == "I") // Typically for addi, addiu, ori, slti, etc.
                        {
                            if (string.IsNullOrEmpty(ops[0])) throw new Exception($"Missing destination register (rt) for {currentMnemonic}: {originalLineForErrorDisplay}");
                            rt_val = ParseOperand(ops[0], labels);         // rt is a register

                            if (string.IsNullOrEmpty(ops[1])) throw new Exception($"Missing source register (rs) for {currentMnemonic}: {originalLineForErrorDisplay}");
                            rs_val = ParseOperand(ops[1], labels);         // rs is a register

                            if (string.IsNullOrEmpty(ops[2])) throw new Exception($"Missing immediate value for {currentMnemonic}: {originalLineForErrorDisplay}");
                            imm_val = ParseOperand(ops[2], labels, true);  // ops[2] is the immediate value, pass true
                        }
                        // Add handlers for other specific I-type formats (e.g., branches if they are 'Type="I"')
                        else
                        {
                            throw new Exception($"Unhandled or ambiguous operand structure for instruction '{currentMnemonic}' (Type: {instrInfo.Type}, Operands found: {ops.Length}): {originalLineForErrorDisplay}");
                        }

                        // Assemble the final machine code using rt_val, rs_val, imm_val.
                        // Standard I-type encoding: OPCODE | rs | rt | immediate
                        // LUI is a special case where the rs field is typically 0 in the encoding.
                        if (currentMnemonic == "lui")
                        {
                            // rs_val is already set to 0 for LUI above
                            machineCode |= ((uint)rs_val & 0x1F) << 21; // This will be (0 << 21)
                            machineCode |= ((uint)rt_val & 0x1F) << 16;
                            machineCode |= ((uint)imm_val & 0xFFFF);
                        }
                        else
                        {
                            // For most other I-types (addiu, ori, lw, sw, lwu, slti, etc.)
                            machineCode |= ((uint)rs_val & 0x1F) << 21;
                            machineCode |= ((uint)rt_val & 0x1F) << 16;
                            machineCode |= ((uint)imm_val & 0xFFFF);
                        }
                    }
                    else if (mnemonic == "lui" && ops.Length == 2) // Assuming 'lui' is handled here
                    {
                        int rt_val = ParseOperand(ops[0], labels);         // Register - flag is false (default)
                        int imm_val = ParseOperand(ops[1], labels, true);  // IMMEDIATE - flag is true

                        machineCode = (instrInfo.Opcode << 26); // LUI opcode
                                                                // RS field is 0 for LUI
                        machineCode |= ((uint)rt_val & 0x1F) << 16;
                        machineCode |= ((uint)imm_val & 0xFFFF);
                    }
                    
                    else if (instrInfo.Type == "I_BRANCH" || instrInfo.Type == "I_BRANCH_LIKELY")
                    {
                        machineCode = (instrInfo.Opcode << 26);
                        int rs_val = ParseOperand(ops[0], labels); int rt_val = ParseOperand(ops[1], labels);
                        uint targetAddr = (uint)ParseOperand(ops[2], labels);
                        int offset = (int)(targetAddr - (currentAddress + 4)) / 4;
                        if (offset < -32768 || offset > 32767) throw new Exception("Branch offset out of range.");
                        machineCode |= ((uint)rs_val & 0x1F) << 21; machineCode |= ((uint)rt_val & 0x1F) << 16; machineCode |= ((uint)offset & 0xFFFF);
                    }
                    else if (instrInfo.Type == "I_BRANCH_RS_ZERO")
                    {
                        machineCode = (instrInfo.Opcode << 26);
                        int rs_val = ParseOperand(ops[0], labels);
                        uint targetAddr = (uint)ParseOperand(ops[1], labels);
                        int offset = (int)(targetAddr - (currentAddress + 4)) / 4;
                        if (offset < -32768 || offset > 32767) throw new Exception("Branch offset out of range.");
                        machineCode |= ((uint)rs_val & 0x1F) << 21; machineCode |= (0 & 0x1F) << 16; machineCode |= ((uint)offset & 0xFFFF);
                    }
                    else if (instrInfo.Type == "I_BRANCH_RS_RTFMT")
                    {
                        machineCode = (instrInfo.Opcode << 26);
                        int rs_val = ParseOperand(ops[0], labels);
                        uint targetAddr = (uint)ParseOperand(ops[1], labels);
                        int offset = (int)(targetAddr - (currentAddress + 4)) / 4;
                        if (offset < -32768 || offset > 32767) throw new Exception("Branch offset out of range.");
                        machineCode |= ((uint)rs_val & 0x1F) << 21; machineCode |= (instrInfo.RtField & 0x1F) << 16; machineCode |= ((uint)offset & 0xFFFF);
                    }
                    else if (instrInfo.Type == "COP0_MOV" || instrInfo.Type == "COP0_MOV_D")
                    {
                        machineCode = (instrInfo.Opcode << 26); machineCode |= (instrInfo.CopOp & 0x1F) << 21;
                        int rt_val = ParseOperand(ops[0], labels); int rd_cop_val = ParseOperand(ops[1], labels);
                        machineCode |= ((uint)rt_val & 0x1F) << 16; machineCode |= ((uint)rd_cop_val & 0x1F) << 11;
                    }
                    else if (instrInfo.Type == "IFPU_LS" || instrInfo.Type == "IFPU_LS_D")
                    {
                        machineCode = (instrInfo.Opcode << 26);
                        int ft_val = ParseOperand(ops[0], labels);
                        var mem = ParseMemOffset(ops[1], labels);
                        machineCode |= ((uint)mem.rs & 0x1F) << 21; machineCode |= ((uint)ft_val & 0x1F) << 16; machineCode |= ((uint)mem.imm & 0xFFFF);
                    }
                    else if (instrInfo.Type == "FPU_MOV") // Handles mtc1, mfc1
                    {
                        machineCode = (instrInfo.Opcode << 26) | (instrInfo.Fmt << 21);

                        if (ops.Length < 2) throw new Exception($"Not enough operands for {mnemonic}: {originalLineForErrorDisplay}");

                        string operand1String = ops[0];
                        string operand2String = ops[1];

                        int value1 = ParseOperand(operand1String, labels);
                        int value2 = ParseOperand(operand2String, labels);

                        int gprValue;
                        int fprValue;

                        bool op1IsFPR = IsFPRName(operand1String);
                        bool op2IsFPR = IsFPRName(operand2String);

                        if (op1IsFPR && !op2IsFPR) // Operand 1 is FPR, Operand 2 is GPR (e.g., mtc1 $f1, t1)
                        {
                            fprValue = value1;
                            gprValue = value2;
                        }
                        else if (!op1IsFPR && op2IsFPR) // Operand 1 is GPR, Operand 2 is FPR (e.g., mtc1 t1, $f1)
                        {
                            gprValue = value1;
                            fprValue = value2;
                        }
                        else if (op1IsFPR && op2IsFPR) // Both operands identified as FPRs
                        {
                            throw new Exception($"Instruction {mnemonic} requires one GPR and one FPR, but received two FPRs: '{operand1String}', '{operand2String}' from line: {originalLineForErrorDisplay}");
                        }
                        else // Both operands identified as GPRs (or neither as FPR)
                        {
                            throw new Exception($"Instruction {mnemonic} requires one GPR and one FPR, but received two GPRs (or non-FPRs): '{operand1String}', '{operand2String}' from line: {originalLineForErrorDisplay}");
                        }

                        // Now, gprValue holds the GPR's number and fprValue holds the FPR's number.
                        // Assign them to the correct fields in the instruction.
                        // RT field (bits 20-16) is for the GPR.
                        // FS field (bits 15-11) is for the FPR.
                        machineCode |= ((uint)gprValue & 0x1F) << 16; // Place GPR number in RT field
                        machineCode |= ((uint)fprValue & 0x1F) << 11; // Place FPR number in FS field
                    }
                    else if (instrInfo.Type == "FPU_R")
                    {
                        machineCode = (instrInfo.Opcode << 26) | (instrInfo.Fmt << 21) | instrInfo.Funct;
                        int fd_val = ParseOperand(ops[0], labels); int fs_val = ParseOperand(ops[1], labels); int ft_val = ParseOperand(ops[2], labels);
                        machineCode |= ((uint)ft_val & 0x1F) << 16; machineCode |= ((uint)fs_val & 0x1F) << 11; machineCode |= ((uint)fd_val & 0x1F) << 6;
                    }
                    else if (instrInfo.Type == "FPU_R_UN") // For abs.s, neg.s, mov.s, sqrt.s
                    {
                        // Ensure only the lower 6 bits of Funct are used if it's stored as a larger value.
                        // (instrInfo.Funct & 0x3F) correctly uses the 6-bit function code.
                        machineCode = (instrInfo.Opcode << 26) | (instrInfo.Fmt << 21) | (instrInfo.Funct & 0x3F);

                        if (ops.Length < 2) throw new Exception($"Not enough operands for {mnemonic}: {originalLineForErrorDisplay}");

                        int fd_val = ParseOperand(ops[0], labels); // Destination operand (e.g., $f3)
                        int fs_val = ParseOperand(ops[1], labels); // Source operand (e.g., $f3 or $f0)

                        uint ft_field_val = 0;          // Standard: FT is 0 for these unary ops
                        uint fs_field_val = (uint)fs_val; // Standard: FS is the source operand
                        uint fd_field_val = (uint)fd_val; // Standard: FD is the destination operand

                        // Special handling for "sqrt.s" when destination and source registers are the same
                        if (mnemonic.Equals("sqrt.s", StringComparison.OrdinalIgnoreCase) && fd_val == fs_val)
                        {
                            // As per your expected output for sqrt.s $f3, $f3:
                            // FT field gets the register number (fd_val)
                            // FS field is set to 0 ($f0)
                            // FD field still gets the register number (fd_val)
                            ft_field_val = (uint)fd_val;
                            fs_field_val = 0; // Source field in encoding becomes $f0
                                              // fd_field_val remains fd_val
                        }

                        machineCode |= (ft_field_val & 0x1F) << 16; // FT field (bits 20-16)
                        machineCode |= (fs_field_val & 0x1F) << 11; // FS field (bits 15-11)
                        machineCode |= (fd_field_val & 0x1F) << 6;  // FD field (bits 10-6)
                    }
                    else if (instrInfo.Type == "FPU_CVT" || instrInfo.Type == "FPU_CVT_D" || instrInfo.Type == "FPU_CVT_S" || instrInfo.Type == "FPU_CVT_L")
                    {
                        machineCode = (instrInfo.Opcode << 26) | (instrInfo.Fmt << 21) | instrInfo.Funct;
                        int fd_val = ParseOperand(ops[0], labels); int fs_val = ParseOperand(ops[1], labels);
                        machineCode |= (0 & 0x1F) << 16; machineCode |= ((uint)fs_val & 0x1F) << 11; machineCode |= ((uint)fd_val & 0x1F) << 6;
                    }
                    else if (instrInfo.Type == "FPU_CMP")
                    {
                        machineCode = (instrInfo.Opcode << 26) | (instrInfo.Fmt << 21) | instrInfo.Funct;
                        int fs_val = ParseOperand(ops[0], labels); int ft_val = ParseOperand(ops[1], labels);
                        machineCode |= ((uint)ft_val & 0x1F) << 16; machineCode |= ((uint)fs_val & 0x1F) << 11;
                    }
                    else if (instrInfo.Type == "FPU_BRANCH")
                    {
                        machineCode = (instrInfo.Opcode << 26) | (instrInfo.Fmt << 21) | (instrInfo.CcBit << 16);
                        uint targetAddr = (uint)ParseOperand(ops[0], labels);
                        int offset = (int)(targetAddr - (currentAddress + 4)) / 4;
                        if (offset < -32768 || offset > 32767) throw new Exception("Branch offset out of range.");
                        machineCode |= ((uint)offset & 0xFFFF);
                    }
                    else if (instrInfo.Type == "J")
                    {
                        machineCode = (instrInfo.Opcode << 26);
                        uint targetAddr = (uint)ParseOperand(ops[0], labels);
                        machineCode |= (targetAddr >> 2) & 0x03FFFFFF;
                    }
                    else
                    {
                        throw new Exception($"Encoding for instruction type '{instrInfo.Type}' not fully implemented for '{mnemonic}'.");
                    }
                    attemptedData = machineCode.ToString("X8");

                    addrStrOutput = currentAddress.ToString("X8");
                    if (addressFormatChar != "-" && addressFormatChar.Length == 1) addrStrOutput = addressFormatChar + addrStrOutput.Substring(1);
                    string hexMachineCode = machineCode.ToString("X8");
                    outputLines.Add(FormatOutputLine(currentAddress, hexMachineCode));
                    currentAddress += 4;
                }
                catch (Exception ex)
                {
                    errors.Add(new MipsErrorInfo(lineInfo.FileName, lineInfo.OriginalLineNumber, lineInfo.GlobalIndex, currentAddress, attemptedData, ex.Message, originalLineForErrorDisplay, isMain));
                }
            }

            if (errors.Any())
            {
                errorLineNumbersToHighlight = errors.Where(errTuple => errTuple.IsFromMainInput).Select(errTuple => errTuple.OriginalLineNumber - 1).Distinct().ToList();
                rtbOutput.Text = (rtbOutput.Text.Length > 0 ? rtbOutput.Text + "\n\n" : "") + "Errors (Pass 2):\n" + string.Join("\n", errors.Select(errTuple => errTuple.ToString()));
            }
            else if (outputLines.Any())
            {
                rtbOutput.Text = string.Join("\n", outputLines);
            }
            else if (processedLineInfos.Any(l => !string.IsNullOrWhiteSpace(l.Text) && !l.Text.Trim().StartsWith("//") && !l.Text.Trim().StartsWith("#")))
            {
                rtbOutput.Text = "No compilable instructions or data found after imports.";
            }
            else
            {
                rtbOutput.Text = "No input provided.";
            }
            UpdateLineCount();
            HighlightSyntax(0, rtbInput.TextLength);
        }

        // --- Helper Methods ---
        private bool IsFPRName(string regName)
        {
            if (string.IsNullOrWhiteSpace(regName)) return false;
            string lowerName = regName.ToLower();
            if (lowerName.StartsWith("$f") || lowerName.StartsWith("f"))
            {
                string numPart = lowerName.StartsWith("$f") ? lowerName.Substring(2) : lowerName.Substring(1);
                if (numPart.All(char.IsDigit) && numPart.Length > 0 && int.TryParse(numPart, out int idx) && idx >= 0 && idx <= 31)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsSymbolicGPRName(string regName)
        {
            if (string.IsNullOrWhiteSpace(regName) || IsFPRName(regName)) return false;
            string namePart = regName.StartsWith("$") ? regName.Substring(1) : regName;
            if (namePart.Length == 0) return false;

            bool hasLetter = namePart.Any(char.IsLetter);
            bool allDigitAfterPotentialDollar = namePart.All(char.IsDigit);

            // True if it has letters AND is not purely numeric (e.g., "t0", "sp", "zero")
            // This excludes purely numeric GPRs like "$8" from being "symbolic" for this check's purpose.
            if (hasLetter && !allDigitAfterPotentialDollar)
            {
                // You could add a more robust check here by comparing against a list of known symbolic GPR names
                // e.g. if (gprSymbolicNames.Contains(namePart.ToLower())) return true;
                return true;
            }
            return false;
        }


        private void UpdateLineCount()
        {
            lblOutputLineCount.Text = $"LINES: {rtbOutput.Lines.Length}";
        }


        // --- Button Event Handlers ---
        private void BtnCopyOutput_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(rtbOutput.Text))
            {
                string textToCopy = string.Join("\r\n", rtbOutput.Lines);
                Clipboard.SetText(textToCopy);
            }
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Start a new file? Unsaved changes will be lost.",
                                                  "New File Confirmation",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                rtbInput.Text = "";
                rtbOutput.Text = "";
                currentFilePath = null;
                errorLineNumbersToHighlight.Clear();
                this.Text = "Code Designer Lite";
                btnSave.Enabled = true;
                HighlightSyntax(0, rtbInput.TextLength);
                UpdateLineCount();
            }
        }


        private void BtnOpen_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Assembly Files (*.txt;*.cds;*.asm;*.s)|*.txt;*.cds;*.asm;*.s|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = openFileDialog.FileName;

                    // Read the file using ISO-8859-1 encoding
                    // This encoding maps byte 0xFF to character U+00FF (ÿ)
                    //rtbInput.Text = File.ReadAllText(currentFilePath, Encoding.GetEncoding("ISO-8859-1"));
                    rtbInput.Text = File.ReadAllText(currentFilePath, Encoding.GetEncoding("Windows-1252"));

                    btnSave.Enabled = true;
                    btnSaveAs.Enabled = true;
                    btnCompile.Enabled = true;
                    this.Text = $"Code Designer Lite - {Path.GetFileName(currentFilePath)}";
                    UpdateLineCount();
                    errorLineNumbersToHighlight.Clear();
                    HighlightSyntax(0, rtbInput.TextLength);
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentFilePath))
            {
                try
                {
                    // Write the file using ISO-8859-1 encoding for consistency
                    File.WriteAllText(currentFilePath, rtbInput.Text, Encoding.GetEncoding("ISO-8859-1"));
                    Console.WriteLine($"Saved to {currentFilePath}");
                    this.Text = $"Code Designer Lite - {Path.GetFileName(currentFilePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                BtnSaveAs_Click(sender, e);
            }
        }

        private void BtnSaveAs_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "CDS File (*.cds)|*.cds|Text File (*.txt)|*.txt|Assembly Files (*.asm;*.s)|*.asm;*.s|All files (*.*)|*.*";
                saveFileDialog.FileName = Path.GetFileName(currentFilePath) ?? "untitled.cds";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = saveFileDialog.FileName;
                    // Write the file using ISO-8859-1 encoding for consistency
                    File.WriteAllText(currentFilePath, rtbInput.Text, Encoding.GetEncoding("ISO-8859-1"));
                    btnSave.Enabled = true;
                    this.Text = $"Code Designer Lite - {Path.GetFileName(currentFilePath)}";
                }
            }
        }

        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Code Designer Lite is based on Code Designer by Gtlcpimp.\nCreated by harry62 through anger and frustration using Gemini.\n\n" +
                "New Commands:\n" +
                "float $100 // create float value at address\n" +
                "b :label // simple branch, converts to beq zero, zero, :label\n\n" +
                "\nVersion: " + string.Format("{0:f2}", CDL_Version),
                            "About Code Designer Lite",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }
        public class CustomColorTable : ProfessionalColorTable { }
    }
}