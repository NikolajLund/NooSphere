﻿/****************************************************************************
 (c) 2012 Steven Houben(shou@itu.dk) and Søren Nielsen(snielsen@itu.dk)

 Pervasive Interaction Technology Laboratory (pIT lab)
 IT University of Copenhagen

 This library is free software; you can redistribute it and/or 
 modify it under the terms of the GNU GENERAL PUBLIC LICENSE V3 or later, 
 as published by the Free Software Foundation. Check 
 http://www.gnu.org/licenses/gpl.html for details.
****************************************************************************/

using System;
using System.Globalization;
using NooSphere.Core.Primitives;

namespace NooSphere.Core.ActivityModel
{
    public class Resource : Base
    {
        public Resource()
        {
            InitializeTimeStamps();
        }
        public Resource(string filePath,string name)
        {
            InitializeTimeStamps();
            Name = name;
            FileName = filePath;
        }

        private void InitializeTimeStamps()
        {
            CreationTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            LastWriteTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }

        public Guid ActivityId { get; set; }
        public Guid ActionId { get; set; }
        public int Size { get; set; }
        public string CreationTime { get; set; }
        public string LastWriteTime { get; set; }
        public string FileName { get; set; }
        public string RelativePath { get {return "Activities/" + ActionId + "/Actions/" + ActionId + "/Resources/" + FileName; }}
        public string CloudPath { get { return "Activities/" + ActionId + "/Actions/" + ActionId + "/Resources/" + Id; } }
        public Service Service { get; set; }
    }
}
