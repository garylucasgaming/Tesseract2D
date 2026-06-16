using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Editor
{
    public partial class TextSearchBarControl : UserControl
    {
        // Expose a standard event so parent panels can react when text changes
        [Category("Action")]
        [Description("Fires when the search text changes, passing the sanitized search string.")]
        public event EventHandler<string>? SearchTextChanged;

        private const string PlaceholderText = "Search...";
        private bool _isPlaceholderActive = true;

        public TextSearchBarControl()
        {
            InitializeComponent();
            SetupPlaceholder();

            // Wire up the internal TextBox events
            textSearchInput.TextChanged += OnTextBoxTextChanged;
            textSearchInput.Enter += OnTextBoxEnter;
            textSearchInput.Leave += OnTextBoxLeave;
        }

        // Public property to read the actual search query safely from outside
        [Browsable(false)] // Hide it from the WinForms property inspector panel
        public string SearchQuery => _isPlaceholderActive ? string.Empty : textSearchInput.Text.Trim();

        private void SetupPlaceholder()
        {
            _isPlaceholderActive = true;
            textSearchInput.Text = PlaceholderText;
            textSearchInput.ForeColor = SystemColors.GrayText;
        }

        // FIX: Added '?' to object to accept nullable sender signatures
        private void OnTextBoxEnter(object? sender, EventArgs e)
        {
            if(_isPlaceholderActive)
            {
                _isPlaceholderActive = false;
                textSearchInput.Text = string.Empty;
                textSearchInput.ForeColor = SystemColors.WindowText;
            }
        }

        // FIX: Added '?' to object
        private void OnTextBoxLeave(object? sender, EventArgs e)
        {
            if(string.IsNullOrWhiteSpace(textSearchInput.Text))
            {
                SetupPlaceholder();
                SearchTextChanged?.Invoke(this, string.Empty);
            }
        }

        // FIX: Added '?' to object
        private void OnTextBoxTextChanged(object? sender, EventArgs e)
        {
            if(_isPlaceholderActive)
                return;
            SearchTextChanged?.Invoke(this, textSearchInput.Text.Trim().ToLower());
        }

        public void Clear()
        {
            SetupPlaceholder();
            SearchTextChanged?.Invoke(this, string.Empty);
        }
    }
}

