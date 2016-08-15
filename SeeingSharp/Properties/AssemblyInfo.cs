﻿#region License information (SeeingSharp and all based games/applications)
/*
    Seeing# and all games/applications distributed together with it. 
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
using System.Reflection;
using System.Resources;

// Custom namespace mapping for wpf
#if DESKTOP
[assembly: System.Windows.Markup.XmlnsDefinition("http://www.rolandk.de/wp/seeingsharp/base", "SeeingSharp.View")]
[assembly: System.Windows.Markup.XmlnsDefinition("http://www.rolandk.de/wp/seeingsharp/base", "SeeingSharp.Infrastructure")]
#endif

[assembly: AssemblyTitle("Seeing#")]
[assembly: AssemblyDescription("Main assembly containing all base classes of Seeing#")]
[assembly: NeutralResourcesLanguage("de")]