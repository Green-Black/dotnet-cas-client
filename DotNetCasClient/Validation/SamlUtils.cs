﻿/*
 * Licensed to Jasig under one or more contributor license
 * agreements. See the NOTICE file distributed with this work
 * for additional information regarding copyright ownership.
 * Jasig licenses this file to you under the Apache License,
 * Version 2.0 (the "License"); you may not use this file
 * except in compliance with the License. You may obtain a
 * copy of the License at:
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on
 * an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied. See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System;
using System.Collections.Generic;
using System.Xml;
using DotNetCasClient.Utils;
using log4net;

namespace DotNetCasClient.Validation
{
    /// <summary>
    /// Utility methods for processing SAML entities, such as the Assertion in a
    /// SAML 1.1 response from a CAS server.
    /// </summary>
    internal sealed class SamlUtils
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private SamlUtils()
        {
        }

#if DOT_NET_3
        /// <summary>
        /// Determines whether the SAML Assertion is valid in terms of the
        /// 'not before' and the 'not on or after' times.
        /// </summary>
        /// <param name="assertion">the SAML Assertion to be checked</param>
        /// <param name="toleranceTicks">
        /// Tolerance ticks for checking the current time against the SAML Assertion
        /// valid times.
        /// </param>
        /// <returns>
        /// true if this Assertion is valid relative to the current time; otherwise
        /// returns false
        /// </returns>
        public static bool IsValidAssertion(System.IdentityModel.Tokens.SamlAssertion assertion, long toleranceTicks)
        {
            DateTime notBefore = assertion.Conditions.NotBefore;
            DateTime notOnOrAfter = assertion.Conditions.NotOnOrAfter;

            return IsValidAssertion(notBefore, notOnOrAfter, toleranceTicks);
        }


        /// <summary>
        ///  Populates an IList with the Values from a SAML Attribute.
        /// </summary>
        /// <param name="attribute">the SAML Attribute to be parsed</param>
        /// <returns>
        /// the IList of existing values, which will be an empty List if no
        /// values are found
        /// </returns>
        public static IList<string> GetValuesFor(System.IdentityModel.Tokens.SamlAttribute attribute)
        {
            IList<string> values = new List<string>();
            foreach (string value in attribute.AttributeValues)
            {
                values.Add(value);
            }
            return values;
        }


        /// <summary>
        /// Retrieves the Authentication Statement from a SAML Assertion.
        /// </summary>
        /// <param name="assertion">the SAML Assertion to be parsed</param>
        /// <returns>
        /// the Authentication Statement for this Assertion if one exists; otherwise
        /// returns null.
        /// </returns>
        public static System.IdentityModel.Tokens.SamlAuthenticationStatement GetSAMLAuthenticationStatement(System.IdentityModel.Tokens.SamlAssertion assertion)
        {
            IEnumerable<System.IdentityModel.Tokens.SamlStatement> samlStmtEnum = assertion.Statements;
            Type targetTypeAuth = typeof(System.IdentityModel.Tokens.SamlAuthenticationStatement);
            
            int stmtCount = 0;            
            foreach (Object obj in samlStmtEnum)
            {
                stmtCount++;
                System.IdentityModel.Tokens.SamlStatement nextStmt = (System.IdentityModel.Tokens.SamlStatement) obj;
                if (nextStmt.GetType().Equals(targetTypeAuth))
                {
                    return (System.IdentityModel.Tokens.SamlAuthenticationStatement) nextStmt;
                }
            }

            return null;
        }

        /// <summary>
        /// Populates an IList with the Attributes from the SAML Assertion for the
        /// given SAML subject.
        /// </summary>
        /// <param name="assertion">the SAML Assertion to be parsed</param>
        /// <param name="subject">
        /// the SAML Subject for which Attributes are to be retrieved
        /// </param>
        /// <returns>
        /// the IList of matching attributes, which will be an empty List if no
        /// matches are found
        /// </returns>
        public static IList<System.IdentityModel.Tokens.SamlAttribute> GetAttributesFor(System.IdentityModel.Tokens.SamlAssertion assertion, System.IdentityModel.Tokens.SamlSubject subject)
        {
            IList<System.IdentityModel.Tokens.SamlAttribute> attributes = new List<System.IdentityModel.Tokens.SamlAttribute>();
            IEnumerable<System.IdentityModel.Tokens.SamlStatement> samlStmtEnum = assertion.Statements;
            Type targetTypeAttr = typeof(System.IdentityModel.Tokens.SamlAttributeStatement);

            foreach (Object obj in samlStmtEnum)
            {
                System.IdentityModel.Tokens.SamlStatement nextStmt = (System.IdentityModel.Tokens.SamlStatement)obj;
                if (nextStmt.GetType().Equals(targetTypeAttr))
                {
                    System.IdentityModel.Tokens.SamlAttributeStatement attributeStatement = (System.IdentityModel.Tokens.SamlAttributeStatement) nextStmt;
                    if (!(subject.Equals(attributeStatement.SamlSubject) || subject.Name.Equals(attributeStatement.SamlSubject.Name)))
                    {
                        continue;
                    }

                    // Found attributes for this Subject.  Transfer them to List.
                    foreach (System.IdentityModel.Tokens.SamlAttribute nextAttr in attributeStatement.Attributes)
                    {
                        attributes.Add(nextAttr);
                    }
                }
            }
            
            return attributes;
        }
#endif
        /// <summary>
        /// Determines whether the SAML Assertion is valid in terms of the
        /// 'not before' and the 'not on or after' times.
        /// </summary>
        /// <param name="notBefore">
        /// the 'not before' time parsed from the Assertion
        /// </param>
        /// <param name="notOnOrAfter">
        /// the 'not on or after' times parsed from the Assertion
        /// </param>
        /// <param name="toleranceTicks">
        /// Tolerance ticks for checking the current time against the SAML Assertion
        /// valid times.
        /// </param>
        /// <returns>
        /// true if this Assertion is valid relative to the current time; otherwise
        /// returns false
        /// </returns>
        public static bool IsValidAssertion(DateTime notBefore, DateTime notOnOrAfter, long toleranceTicks)
        {
            if (notBefore == DateTime.MinValue || notOnOrAfter == DateTime.MinValue)
            {
                Log.Debug(string.Format("{0}:Assertion has no bounding dates.  Will not process.", CommonUtils.MethodName));
                return false;
            }
            
            long utcNowTicks = DateTime.UtcNow.Ticks;
            
            Log.Debug(string.Format("{0}:compare {1} < {2}", CommonUtils.MethodName, utcNowTicks + toleranceTicks, notBefore.Ticks));

            if (utcNowTicks + toleranceTicks < notBefore.Ticks)
            {
                Log.Debug(string.Format("{0}:skipping assertion that's not yet valid...", CommonUtils.MethodName));
                return false;
            }
            
            if (notOnOrAfter.Ticks <= utcNowTicks - toleranceTicks)
            {
                Log.Debug(string.Format("{0}:skipping expired assertion...", CommonUtils.MethodName));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Populates an IDictionary with the Attributes from the SAML Assertion
        /// for the given SAML Subject.
        /// </summary>
        /// <param name="attributeStmtNode">
        /// the Attribute statement parsed from the SAML Assertion
        /// </param>
        /// <param name="nsmgr">
        /// the XmlNamespaceManager for the input XMLNode containing the SAML
        /// Attribute statement to be parsed
        /// </param>
        /// <param name="subjectName">
        /// the SAML Subject for which Attributes are to be retrieved
        /// </param>
        /// <returns>
        /// the IDictionary of matching attributes, which will be an empty
        /// IDictionary if no matches are found.  The key is the Attribute name
        /// and the value is the IList of values for that Attribute, which might
        /// be an empty IList.
        /// </returns>
        /// <exception cref="TicketValidationException">
        /// Thrown if expected entries if the requested SAML subject can not be
        /// parsed from the attrStmtNode.
        /// </exception>
        public static IDictionary<string, IList<string>> GetAttributesFor(XmlNode attributeStmtNode, XmlNamespaceManager nsmgr, string subjectName)
        {
            XmlNode nameIdentifierNode = attributeStmtNode.SelectSingleNode("child::assertion:Subject/child::assertion:NameIdentifier", nsmgr);
            if (nameIdentifierNode == null)
            {
                throw new TicketValidationException("No NameIdentifier found in AttributeStatement of the CAS response.");
            }

            string subject = nameIdentifierNode.FirstChild.Value;
            if (String.IsNullOrEmpty(subjectName) || !subjectName.Equals(subject))
            {
                throw new TicketValidationException(string.Format("AttributeStatement subject ({0}) does not match requested subject ({1}) in the CAS response.", subject, subjectName));
            }

            IDictionary<string, IList<string>> attributes = new Dictionary<string, IList<string>>();
            XmlNodeList attributeNodes = attributeStmtNode.SelectNodes("descendant::assertion:Attribute", nsmgr);
            
            if (attributeNodes != null)
            {
                foreach (XmlNode nextAttr in attributeNodes)
                {
                    XmlAttributeCollection attrs = nextAttr.Attributes;
                    string attrName = GetAttributeValue(attrs, "AttributeName");
                    if (String.IsNullOrEmpty(attrName))
                    {
                        continue;
                    }

                    XmlNodeList attrValuesNodes = nextAttr.ChildNodes;

                    IList<string> values = new List<string>();
                    foreach (XmlNode nextValueNode in attrValuesNodes)
                    {
                        XmlNode textNode = nextValueNode.FirstChild;
                        if (textNode == null)
                        {
                            continue;
                        }

                        string valueText = textNode.Value;
                        if (!String.IsNullOrEmpty(valueText))
                        {
                            values.Add(valueText);
                        }
                    }

                    if (values.Count > 0)
                    {
                        attributes.Add(attrName, values);
                    }
                }
            }

            return attributes;
        }

        /// <summary>
        /// Retrieves the value for the specified attribute name from the
        /// collection of attributes.
        /// </summary>
        /// <param name="attrs">the attributes to process</param>
        /// <param name="attrName">the name of the attribute desired</param>
        /// <returns>
        /// the parsed value if the attribute is found; otherwise null is returned.
        /// </returns>
        public static string GetAttributeValue(XmlAttributeCollection attrs, string attrName)
        {
            if (attrs != null)
            {
                XmlNode attrNode = attrs.GetNamedItem(attrName);
                if (attrNode != null)
                {
                    return attrNode.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieves the value for the specified attribute name and converts it
        /// to a DateTime value.
        /// </summary>
        /// <param name="currentNode">
        /// the node containing the attributes to be processed
        /// </param>
        /// <param name="attrName">the name of the attribute desired</param>
        /// <returns>
        /// the parsed and converted value
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the desired attribute is not found
        /// </exception>
        public static DateTime GetAttributeValueAsDateTime(XmlNode currentNode, string attrName)
        {
            if (currentNode != null)
            {
                XmlAttributeCollection attrColl = currentNode.Attributes;
                if (attrColl != null)
                {
                    string attrValue = GetAttributeValue(attrColl, attrName);
                    if (!String.IsNullOrEmpty(attrValue))
                    {
                        return DateTime.Parse(attrValue).ToUniversalTime();
                    }
                }
            }

            throw new ArgumentNullException(string.Format("No value for >{0}< in XmlNode for DateTime conversion", attrName));
        }
    }
}
