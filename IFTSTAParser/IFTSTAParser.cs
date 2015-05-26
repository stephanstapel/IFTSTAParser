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

namespace s2industries.IFTSTA
{
    public class IFTSTAParser
    {
        private const string HEADER = "UNA:+.?";
        private const string STANDARD_DATEFORMAT = "yyyyMMddHHmm";
        private const string SHORT_DATEFORMAT = "yyyyMMdd";

        public static List<IFTSTAConsigment> Load(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                throw new Exception(String.Format("File '{0}' does not exist", path));
            }

            StreamReader streamReader = new StreamReader(path);
            string data = streamReader.ReadToEnd().Replace("\r", "").Replace("\n", "");
            streamReader.Close();

            // remove header
            if (data.IndexOf(HEADER) > -1)
            {
                data = data.Substring(data.IndexOf(HEADER) + HEADER.Length).Trim();
            }

            List<IFTSTAConsigment> retval = new List<IFTSTAConsigment>();

            // iterate through segments, find CNI elements and evaluate consecutive segments
            List<EDISegment> rawSegments = _SplitIntoSegments(data);
            List<List<EDISegment>> transportStatusSegments = _SplitIntoTransportStatusSegments(rawSegments);

            foreach(List<EDISegment> segments in transportStatusSegments)
            {
                IFTSTAConsigment consigment = new IFTSTAConsigment();

                foreach(EDISegment segment in segments)
                {
                    switch (segment.Qualififier.Trim().ToUpper())
                    {
                        case "CNI": // consigment
                        {
                            consigment.No = segment.GetElement(1);
                            break;
                        }
                        case "GIN": // global identifier
                        {
                            consigment.GlobalIdentifier = segment.GetElement(1);
                            break;
                        }
                        case "DTM": // datetime
                        {
                            string _dateformat = STANDARD_DATEFORMAT; // qualifier 203 = default
                            if (segment.GetElement(2) == "102")
                            {
                                _dateformat = SHORT_DATEFORMAT;
                            }


                            if (segment.GetElement(0) == "334")
                            {
                                string s = segment.GetElement(1);

                                consigment.StatusChangeDate = DateTime.ParseExact(segment.GetElement(1),
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
                                consigment.Status = segment.GetElement(1);
                            }
                            break;
                        }
                        case "FTX": // free text
                        {
                            // join free text into one string
                            consigment.AdditionalInfo = "";
                            for (int k = 1; k < segment.DataElements.Count; k++)
                            {
                                if (!String.IsNullOrEmpty(segment.GetElement(k)))
                                {
                                    if (consigment.AdditionalInfo.Length > 0)
                                    {
                                        consigment.AdditionalInfo += ", ";
                                    }
                                    consigment.AdditionalInfo += segment.GetElement(k);
                                }
                            } // !for(k)
                            break;
                        }
                        default:
                        {
                            break;
                        }
                    }
                } // !for(i)

                retval.Add(consigment);
            } // !foreach(transportStatusSegments)

            return retval;
        } // !Load()


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
                List<string> data = element.Substring(pos + 1).Split(new char[] { ':', '+' }).ToList();

                retval.Add(new EDISegment(token, data));
            }

            return retval;
        } // !_SplitIntoSegments()


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
