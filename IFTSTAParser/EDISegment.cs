﻿/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */
using IFTSTAParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace s2industries.IFTSTA
{
    public class EDISegment
    {
        public string Qualififier { get; private set; }
        public List<EDIDataElement> DataElements { get; set; }


        public EDISegment(string qualifier, List<EDIDataElement> dataElements = null)
        {
            this.Qualififier = qualifier;
            this.DataElements = dataElements;
        }

        public EDIDataElement GetElement(int index)
        {
            if (index >= this.DataElements.Count)
            {
                return null;
            }
            else
            {
                return this.DataElements[index];
            }
        } // !GetElement()
    }
}
