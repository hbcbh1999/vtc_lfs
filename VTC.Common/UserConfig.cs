﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace VTC.Common
{
    [DataContract]
    public class UserConfig
    {
        [Description("Remote server username.")]
        [DataMember]
        public string Username { get; set; }

        [Description("Remote server password.")]
        [DataMember]
        public string Password { get; set; }

        [Description("Path to logo image for display in GUI.")]
        [DataMember]
        public string Logopath { get; set; }

        [Description("Organization name for display in GUI.")]
        [DataMember]
        public string Organization { get; set; }

        [Description("Camera-1 URL for RTSP stream.")]
        [DataMember]
        public string Camera1Url { get; set; }

        [Description("Camera-1 name.")]
        [DataMember]
        public string Camera1Name { get; set; }        

        [Description("Camera-2 URL for RTSP stream.")]
        [DataMember]
        public string Camera2Url { get; set; }

        [Description("Camera-2 name.")]
        [DataMember]
        public string Camera2Name { get; set; }        

        [Description("Camera-3 URL for RTSP stream.")]
        [DataMember]
        public string Camera3Url { get; set; }

        [Description("Camera-3 name.")]
        [DataMember]
        public string Camera3Name { get; set; }        

        [Description("Display counts only based on approaches in GUI.")]
        [DataMember]
        public string SimplifiedCountDisplay { get; set; }        

        [Description("URL of remote server for recieving measurements.")]
        [DataMember]
        public string ServerUrl { get; set; }        
    }
}