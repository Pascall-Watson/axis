using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Input;

namespace ExcelExporterImporter.Views
{
    public partial class ButtonUserControl : UserControl
    {
        public ButtonUserControl()
        {
            InitializeComponent();
            button.Click += (sender, args) => OnButtonClick();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ButtonText
        {
            get => button.Text;
            set => button.Text = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ICommand Command { get; set; }

        private void OnButtonClick()
        {
            if (Command.CanExecute(null)) Command.Execute(null);
        }
    }
}