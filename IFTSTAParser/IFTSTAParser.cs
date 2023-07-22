/*
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Net.Http.Headers;
using IFTSTAParser;

namespace s2industries.IFTSTA
{
    public class IFTSTAParser
    {
        private const string HEADER = "UNA:+.?";
        private const string STANDARD_DATEFORMAT = "yyyyMMddHHmm";
        private const string SHORT_DATEFORMAT = "yyyyMMdd";

        public static IFTSTADocument Load(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new Exception(String.Format("File '{0}' does not exist", path));
            }

            StreamReader streamReader = new StreamReader(path);
            string data = streamReader.ReadToEnd();
            streamReader.Close();

            return LoadFromString(data);
        } // !Load()


        public static IFTSTADocument LoadFromString(string data)
        {
            // clean data
            data = data.Replace("\r", "").Replace("\n", "");

            // remove header
            if (data.IndexOf(HEADER) > -1)
            {
                data = data.Substring(data.IndexOf(HEADER) + HEADER.Length).Trim();
            }

            List<IFTSTAConsigment> consigments = new List<IFTSTAConsigment>();

            // iterate through segments, find CNI elements and evaluate consecutive segments
            List<EDISegment> rawSegments = _SplitIntoSegments(data);
            List<EDISegment> headerSegments = _GetHeaderSegments(rawSegments);
            List<EDISegment> dataSegments = _GetDataSegments(rawSegments);
            List<List<EDISegment>> transportStatusSegments = _SplitIntoTransportStatusSegments(rawSegments);

            EDISegment unbSegment = headerSegments.FirstOrDefault(s => s.Qualififier == "UNB");
            DateTime? creationDate = null;
            if (unbSegment != null)
            {
                // data:
                // element 0: SYNTAX IDENTIFIER
                // element 1: INTERCHANGE SENDER
                // element 2: INTERCHANGE RECIPIENT
                // element 3: DATE AND TIME OF PREPARATION

                string date = unbSegment.GetElement(3).GetValue(0);
                string time = unbSegment.GetElement(3).GetValue(1);

                if ((date.Length == 6) && (time.Length == 4))
                {
                    creationDate = new DateTime(
                        2000 + Int32.Parse(date.Substring(0, 2)),
                        Int32.Parse(date.Substring(2, 2)),
                        Int32.Parse(date.Substring(4, 2)),
                        Int32.Parse(time.Substring(0, 2)),
                        Int32.Parse(time.Substring(2, 2)),
                        0
                    );
                }
                else if ((date.Length == 8) && (time.Length == 4))
                {
                    creationDate = new DateTime(
                        Int32.Parse(date.Substring(0, 4)),
                        Int32.Parse(date.Substring(4, 2)),
                        Int32.Parse(date.Substring(6, 2)),
                        Int32.Parse(time.Substring(0, 2)),
                        Int32.Parse(time.Substring(2, 2)),
                        0
                    );
                }
            }

            // analyze data
            foreach (List<EDISegment> segments in transportStatusSegments)
            {
                IFTSTAConsigment consigment = new IFTSTAConsigment();
                    
                foreach(EDISegment segment in segments)
                {
                    switch (segment.Qualififier.Trim().ToUpper())
                    {
                        case "CNI": // consigment
                        {
                            segment.GetElement(0).GetValue(0); // Serial number differentiating each separate consignment included in the status report.
                            consigment.No = segment.GetElement(1).GetValue(0); // Consignor's shipment reference number
                            break;
                        }
                        case "GIN": // global identifier
                        {
                            consigment.GlobalIdentifier = segment.GetElement(1).GetValue(0);
                            break;
                        }
                        case "DTM": // datetime
                        {
                            string _dateformat = STANDARD_DATEFORMAT; // qualifier 203 = default
                            if (segment.GetElement(0).GetValue(2) == "102")
                            {
                                _dateformat = SHORT_DATEFORMAT;
                            }


                            if (segment.GetElement(0).GetValue(0) == "334")
                            {
                                string s = segment.GetElement(0).GetValue(1);

                                consigment.StatusChangeDate = DateTime.ParseExact(segment.GetElement(0).GetValue(1),
                                                                                _dateformat,
                                                                                CultureInfo.InvariantCulture,
                                                                                DateTimeStyles.None);
                            }
                            break;
                        }
                        case "STS": // status
                        {
                            if (segment.DataElements.Count == 2)
                            {
                                consigment.Status = segment.GetElement(1).GetValue(0);
                            }
                            break;
                        }
                        case "FTX": // free text
                        {
                            // join free text into one string
                            consigment.AdditionalInfo = segment.GetElement(3)?.GetValue(0);
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                } // !for(i)

                consigments.Add(consigment);
            } // !foreach(transportStatusSegments)

            return new IFTSTADocument()
            {
                CreationDate = creationDate,
                Consigments = consigments
            };
        } // !LoadFromString()


        private static List<EDISegment> _SplitIntoSegments(string rawData)
        {
            List<string> tempElements = rawData.Split(new char[] { '\'' }).ToList();

            List<EDISegment> retval = new List<EDISegment>();
            foreach (string element in tempElements)
            {
                if (element.IndexOf("+") == -1)
                {
                    continue;
                }

                int pos = element.IndexOf("+");
                string token = element.Substring(0, pos);
                List<EDIDataElement> dataElements = new List<EDIDataElement>();
                foreach (string data in element.Substring(pos + 1).Split(new char[] { '+' }))
                {
                    List<string> _values = data.Split(new char[] { ':' }).ToList();
                    dataElements.Add(new EDIDataElement()
                    {
                        Values = _values
                    });
                }
                retval.Add(new EDISegment(token, dataElements));
            }

            return retval;
        } // !_SplitIntoSegments()


        private static List<EDISegment> _GetHeaderSegments(List<EDISegment> rawSegments)
        {
            List<EDISegment> retval = new List<EDISegment>();
            foreach(EDISegment segment in rawSegments)
            {
                if (segment.Qualififier == "BGM")
                {
                    break;
                }
                retval.Add(segment);
            }

            return retval;
        } // !_GetHeaderSegments()


        private static List<EDISegment> _GetDataSegments(List<EDISegment> rawSegments)
        {
            List<EDISegment> retval = new List<EDISegment>();
            bool bgmAlreadyFound = false;
            foreach (EDISegment segment in rawSegments)
            {
                if (segment.Qualififier == "BGM")
                {
                    bgmAlreadyFound = true;
                }

                if (bgmAlreadyFound)
                {
                    retval.Add(segment);
                }
            }

            return retval;
        } // !_GetDataSegments()


        private static List<List<EDISegment>> _SplitIntoTransportStatusSegments(List<EDISegment> rawSegments)
        {
            List<List<EDISegment>> retval = new List<List<EDISegment>>();

            for (int i = 0; i < rawSegments.Count; i++)
            {
                if (String.Equals(rawSegments[i].Qualififier, "CNI", StringComparison.InvariantCultureIgnoreCase))
                {
                    List<EDISegment> segments = new List<EDISegment>();
                    segments.Add(rawSegments[i++]);

                    while (true)
                    {
                        segments.Add(rawSegments[i]);
                        if ( (i + 1 < rawSegments.Count) && 
                             (String.Equals(rawSegments[i + 1].Qualififier, "CNI", StringComparison.InvariantCultureIgnoreCase)) )
                        {
                            break;
                        }

                        i += 1;
                        if (i >= rawSegments.Count)
                        {
                            break;
                        }
                    }

                    retval.Add(segments);
                }
            } // !for(i)

            return retval;
        } // !_SplitIntoTransportStatusSegments()
    }
}
