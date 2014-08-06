// Copyright (c) 2014 Kuan Bartel, All rights reserved.
// See License.md in the project root for license information.

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TestModel
{
    public class City : INotifyPropertyChanged
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set
            {
                _Name = value;
                OnPropertyChanged();
            }
        }

        private State _State;
        public State State
        {
            get { return _State; }
            set
            {
                _State = value;
                OnPropertyChanged();
            }
        }

        private string _PostCode;
        public string PostCode
        {
            get { return _PostCode; }
            set
            {
                _PostCode = value;
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

    public enum State
    {
        ACT,
        NSW,
        VIC,
        QLD,
        SA,
        WA,
        TAS,
        NT,
    }
}
