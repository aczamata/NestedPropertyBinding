using NestedPropertyBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TestModel;

namespace NestedBindingSample
{
    public partial class Form1 : Form
    {
        NestedBindingList<Person> list;

        public Form1()
        {
            InitializeComponent();

            // Display the enum values properly
            StateColumn.ValueType = typeof(State);
            StateColumn.ValueMember = "Value";
            StateColumn.DisplayMember = "Display";
            StateColumn.DataSource =
                    new List<State>((State[])Enum.GetValues(typeof(State)))
                        .Select(v => new { Display = v.ToString(), Value = v })
                        .ToList();

            // Create a NestedBindingList and set it as the DataSource
            list = new NestedBindingList<Person>(2);
            list.PropertyBindingDepth = 2;
            // Need to create the nested objects otherwise they'll be null
            list.AddingNew += (s, args) =>
                {
                    args.NewObject = new Person()
                    {
                        Address = new Address()
                        {
                            City = new City()
                        }
                    };
                };

            personDataGridView.AutoGenerateColumns = false;
            personDataGridView.DataSource = list;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var p = new Person()
            {
                ID = 1,
                Name = "Fred",
                Address = new Address()
                    {
                        Street = "100 Collins St",
                        City = new City()
                            {
                                Name = "Melbourne",
                                State = State.VIC,
                                PostCode = "3000"
                            }
                    }
            };

            list.Add(p);

            p = new Person()
            {
                ID = 2,
                Name = "Bill",
                Address = new Address()
                {
                    Street = "60 Queen St",
                    City = new City()
                    {
                        Name = "Brisbane",
                        State = State.QLD,
                        PostCode = "4000"
                    }
                }
            };

            list.Add(p);
        }
    }
}
