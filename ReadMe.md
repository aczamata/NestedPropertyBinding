# NestedPropertyBinding

This project provides helper classes for binding to nested properties.
The main impetus for the project was to allow a WinForms DataGridView
to be able to bind to nested properties of any depth.

## Classes

| Class name | Purpose |
| ---------- | ------- |
| NestedBindingList<T> | An [IBindingList](http://msdn.microsoft.com/en-us/library/System.ComponentModel.IBindingList.aspx) which supports nested properties. It supports change notifications, sorting and searching, including for nested properties. |
| NestedPropertyChangedNotifier<T> | An [INotifyPropertyChanged](http://msdn.microsoft.com/en-us/library/System.ComponentModel.INotifyPropertyChanged.aspx) for raising nested PropertyChanged events. |
| NestedPropertyDescriptor | A [PropertyDescriptor](http://msdn.microsoft.com/en-us/library/System.ComponentModel.PropertyDescriptor.aspx) for nested properties of an object. |

## License

All files contained within the project are licensed under the [Apache
License, Version 2](http://www.apache.org/licenses/LICENSE-2.0).
Some code is additionally licensed under the MIT X11 License.
Please see the [License.md](License.md) and [Notice.md](Notice.md) files
for license terms and attribution notices.