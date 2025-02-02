﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# Tools. More info at
     - https://github.com/RolandKoenig/SeeingSharp/tree/master/Tools (sourcecode)
     - http://www.rolandk.de/wp (the autors homepage, german)
    Copyright (C) 2016 Roland König (RolandK)

	This program is distributed under the terms of the Microsoft Public License (Ms-PL)-
	More info at https://msdn.microsoft.com/en-us/library/ff647676.aspx
*/
#endregion License information (SeeingSharp and all based games/applications)

using System;
using System.Globalization;
using System.Windows.Data;

namespace SeeingSharpModelViewer
{
    public class BooleanNegateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool)) { throw new InvalidOperationException("Invalid target Type!"); }
            if (value.GetType() != typeof(bool)) { throw new InvalidOperationException("Invalid source Type!"); }

            return !(bool)(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool)) { throw new InvalidOperationException("Invalid target Type!"); }
            if (value.GetType() != typeof(bool)) { throw new InvalidOperationException("Invalid source Type!"); }

            return !(bool)(value);
        }
    }
}