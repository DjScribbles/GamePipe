/**
* Copyright (c) 2017 Joseph Shaw & Big Sky Software LLC
* Distributed under the GNU GPL v2. For full terms see the file LICENSE.txt
*/

using System;
using System.Globalization;
using System.Windows.Data;
using GamePipe.ViewModel;

namespace GamePipe.Converters
{
    public class TransferBaseToViewModelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GamePipeLib.Model.TransferBase)
                return new TransferViewModel((GamePipeLib.Model.TransferBase)value);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as TransferViewModel)?.Model;
        }
    }
}
