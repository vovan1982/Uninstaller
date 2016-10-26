using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Uninstaller.Model
{
    public class Soft
    {
        private ImageSource _icon;
        private string _name;
        private string _uninstallString;
        private string _iconImagePath;
        private string _version;

        public Soft()
        {
        }

        public ImageSource Icon
        {
            get { return _icon; }
            set { _icon = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string UninstallString
        {
            get { return _uninstallString; }
            set { _uninstallString = value; }
        }

        public string IconImagePath
        {
            get { return _iconImagePath; }
            set { _iconImagePath = value; }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }
    }
}
