﻿///////////////////////////////////////////////////////////////////
//
// Youbiquitous YBQ : app starter 
// Copyright (c) Youbiquitous srls 2017
//
// Author: Dino Esposito (http://youbiquitous.net)
//


namespace Mfx1.Server.Common.Exceptions
{
    public class InvalidGuidException : YbqAppException
    {
        public InvalidGuidException(string message) : base(message)
        {
        }

        public InvalidGuidException(int code, string message) : base(code, message)
        {
        }

    }
}