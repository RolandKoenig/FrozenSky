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

using SeeingSharp;
using SeeingSharp.Util;

namespace SeeingSharpModelViewer
{
    [MessagePossibleSource(SeeingSharpConstants.THREAD_NAME_GUI)]
    public class NewModelLoadedMessage : SeeingSharpMessage
    {
    }
}