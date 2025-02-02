﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
	Exception are projects where it is noted otherwhise.
    More info at 
     - https://github.com/RolandKoenig/SeeingSharp (sourcecode)
     - http://www.rolandk.de/wp (the autors homepage, german)
    Copyright (C) 2016 Roland König (RolandK)
    
    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.
    
    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see http://www.gnu.org/licenses/.
*/
#endregion
using SeeingSharp.Infrastructure;
using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SeeingSharp.Util
{
    public class PropertyChangedBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Raises when one of the public properties have changed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the display name of the property described by the given expression.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="getPropertyExpression"></param>
        public string GetMemberDisplayName<T>(Expression<Func<T>> getPropertyExpression)
        {
            LambdaExpression lambdaExpression = getPropertyExpression as LambdaExpression;
            if (lambdaExpression != null)
            {
                MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
                if (memberExpression != null)
                {
                    TranslatableDisplayNameAttribute attrib = memberExpression.Member.GetCustomAttribute<TranslatableDisplayNameAttribute>();
                    if (attrib != null)
                    {
                        return attrib.DisplayName;
                    }
                    else
                    {
                        return memberExpression.Member.Name;
                    }
                }
            }

            throw new SeeingSharpException("Unable to parse given expression!");
        }

        /// <summary>
        /// Gets the display name of the given property.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        public string GetMemberDisplayName(string propertyName)
        {
            PropertyInfo propertyInfo = this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName);
            if (propertyInfo != null)
            {
                TranslatableDisplayNameAttribute attrib = propertyInfo.GetCustomAttribute<TranslatableDisplayNameAttribute>(); 
                if (attrib != null)
                {
                    return attrib.DisplayName;
                }
            }
            return propertyName;
        }

        /// <summary>
        /// Ensures that the given string is not null,
        /// </summary>
        public string EnsureNotNull(string givenString)
        {
            if (givenString == null) { return string.Empty; }
            return givenString;
        }

        /// <summary>
        /// Gets the name of the member the given expression points to.
        /// </summary>
        /// <typeparam name="T">Type of the mebmer.</typeparam>
        /// <param name="getMemberExpression">The expression pointing to the member.</param>
        public string GetMemberName<T>(Expression<Func<T>> getMemberExpression)
        {
            LambdaExpression lambdaExpression = getMemberExpression as LambdaExpression;
            if (lambdaExpression != null)
            {
                MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
                if (memberExpression != null)
                {
                    return memberExpression.Member.Name;
                }
            }

            throw new InvalidOperationException("Unable to process given expression!");
        }

        /// <summary>
        /// Raises the PropertyChanged event using the member within the given expression.
        /// </summary>
        /// <typeparam name="T">Type of the member.</typeparam>
        /// <param name="getPropertyExpression">The linq epxression to parse.</param>
        protected void RaisePropertyChanged<T>(Expression<Func<T>> getPropertyExpression)
        {
            LambdaExpression lambdaExpression = getPropertyExpression as LambdaExpression;
            if (lambdaExpression != null)
            {
                MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
                if (memberExpression != null)
                {
                    RaisePropertyChanged(memberExpression.Member.Name);
                    return;
                }
            }

            throw new InvalidOperationException("Unable to process given expression!");
        }

        /// <summary>
        /// Gets the property the given expression points to.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="getPropertyExpression">The expression that points to the property.</param>
        protected PropertyInfo GetProperty<T>(Expression<Func<T>> getPropertyExpression)
        {
            LambdaExpression lambdaExpression = getPropertyExpression as LambdaExpression;
            if (lambdaExpression != null)
            {
                MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
                if (memberExpression != null)
                {
                    return memberExpression.Member as PropertyInfo;
                }
            }

            throw new InvalidOperationException("Unable to process given expression!");
        }

        /// <summary>
        /// Gets the foeöd the given expression points to.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="getPropertyExpression">The expression that points to the field.</param>
        protected FieldInfo GetField<T>(Expression<Func<T>> getPropertyExpression)
        {
            LambdaExpression lambdaExpression = getPropertyExpression as LambdaExpression;
            if (lambdaExpression != null)
            {
                MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;
                if (memberExpression != null)
                {
                    return memberExpression.Member as FieldInfo;
                }
            }

            throw new InvalidOperationException("Unable to process given expression!");
        }

        /// <summary>
        /// Notifies a changed property.
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChanged(
            [CallerMemberName]
            string propertyName = "")
        {
            if (string.IsNullOrEmpty(propertyName)) { return; }
            if (PropertyChanged != null) { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); }
        }
    }
}
