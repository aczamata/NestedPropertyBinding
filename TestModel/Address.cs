// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestModel
{
    public class Address : INotifyPropertyChanged
    {
        private string _Street;
        public string Street
        {
            get { return _Street; }
            set
            {
                _Street = value;
                OnPropertyChanged();
            }
        }

        private City _City;
        public City City
        {
            get { return _City; }
            set
            {
                _City = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
