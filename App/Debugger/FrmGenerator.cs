namespace Debugger
{
    public partial class FrmGenerator : Form
    {
        public FrmGenerator()
        {
            InitializeComponent();
        }

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            var b1 = $"{(checkBox1.Checked ? "1" : "0")}{(checkBox2.Checked ? "1" : "0")}" +
                $"{(checkBox3.Checked ? "1" : "0")}{(checkBox4.Checked ? "1" : "0")}" +
                $"{(checkBox5.Checked ? "1" : "0")}{(checkBox6.Checked ? "1" : "0")}" +
                $"{(checkBox7.Checked ? "1" : "0")}{(checkBox8.Checked ? "1" : "0")}";
            var t1 = $"0x{Convert.ToString(Convert.ToByte(b1, 2), 16)}";
            var b2 = $"{(checkBox9.Checked ? "1" : "0")}{(checkBox10.Checked ? "1" : "0")}" +
                $"{(checkBox11.Checked ? "1" : "0")}{(checkBox12.Checked ? "1" : "0")}" +
                $"{(checkBox13.Checked ? "1" : "0")}{(checkBox14.Checked ? "1" : "0")}" +
                $"{(checkBox15.Checked ? "1" : "0")}{(checkBox16.Checked ? "1" : "0")}";
            var t2 = $"0x{Convert.ToString(Convert.ToByte(b2, 2), 16)}";
            txtResult.Text = $"{t1} {t2}";
        }
    }
}
