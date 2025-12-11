using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MPV
{
    public partial class lb_pass : Form
    {
        private Form1 _form1;
        private int _numpass;
        private int _numfail;

        public lb_pass(Form1 form1)
        {
            InitializeComponent();
            _form1 = form1;
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _form1.Show();  // khi Form2 tắt → hiện lại Form1
            base.OnFormClosed(e);
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }

        
    }
}
