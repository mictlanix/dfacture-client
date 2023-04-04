//
// DFactureClient.cs
//
// Author:
//       Eddy Zavaleta <eddy@mictlanix.com>
//
// Copyright (c) 2018 Mictlanix SAS de CV and contributors.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.Text;
using System.Xml;
using Mictlanix.CFDv40;
using Mictlanix.DFacture.Client.Internals;

namespace Mictlanix.DFacture.Client40 {
	public class DFactureClient40 {
		public static string URL_PRODUCTION = @"https://timbradosoap.solucionesdfacture.com/WSTimbradoSOAP.svc";
		public static string URL_TEST = @"http://timbradosoap33.testdfacture.com/WSTimbradoSOAP.svc";

		static readonly BasicHttpBinding binding = new BasicHttpBinding (BasicHttpSecurityMode.Transport) {
			MaxBufferPoolSize = int.MaxValue,
			MaxReceivedMessageSize = int.MaxValue,
			ReaderQuotas = new XmlDictionaryReaderQuotas {
				MaxDepth = int.MaxValue,
				MaxStringContentLength = int.MaxValue,
				MaxArrayLength = int.MaxValue,
				MaxBytesPerRead = int.MaxValue,
				MaxNameTableCharCount = int.MaxValue,
			}
		};

		string url;
		EndpointAddress address;

		public DFactureClient40 (string username, string password) : this (username, password, URL_PRODUCTION)
		{
		}

		public DFactureClient40 (string username, string password, string url)
		{
			Username = username;
			Password = password;
			Url = url;

			if (url.StartsWith ("http://", StringComparison.OrdinalIgnoreCase)) {
				binding.Security.Mode = BasicHttpSecurityMode.None;
			}

			ServicePointManager.ServerCertificateValidationCallback =
				(object sp, X509Certificate c, X509Chain r, SslPolicyErrors e) => true;
		}

		public string Username { get; protected set; }
		public string Password { get; protected set; }

		public string Url {
			get { return url; }
			set {
				if (url == value)
					return;

				url = value;
				address = new EndpointAddress (url);
			}
		}

		public TimbreFiscalDigital Stamp (Comprobante cfd)
		{
			return Stamp (cfd.ToXmlBytes ());
		}

		public TimbreFiscalDigital Stamp (string xml)
		{
			return Stamp (Encoding.UTF8.GetBytes (xml));
		}

		public TimbreFiscalDigital Stamp (byte [] xml)
		{
			return StampBase64String (Convert.ToBase64String (xml));
		}

		/*
		 * TimbradoSoapClient Reponse Array
		 * 
		 * Index 0: Exception type
		 * Index 1: Error number
		 * Index 2: Result description
		 * Index 3: Stamped xml document
		 * Index 4: Byte array for QRCode image
		 * Index 5: Stamp string
		 * 
		 */

		public TimbreFiscalDigital StampBase64String (string base64Xml)
		{
			string xml_response = null;
			TimbreFiscalDigital tfd = null;

			using (var ws = new WSTimbradoSOAPClient (binding, address)) {
				//var response = ws.TimbrarCFDI33 (Username, Password, base64Xml);
				var response = ws.TimbrarCFDI40 (Username, Password, base64Xml);
				string err_number = response.codigo;
				string err_description = response.mensaje;

				if (err_number != "100") {
					throw new DFactureClientException40 (err_number, err_description);
				}

				xml_response = Encoding.UTF8.GetString (Convert.FromBase64String (response.xml));
			}

			var cfd = Comprobante.FromXml (xml_response);

			foreach (var item in cfd.Complemento) {
				if (item is TimbreFiscalDigital) {
					tfd = item as TimbreFiscalDigital;
					break;
				}
			}

			if (tfd == null) {
				throw new DFactureClientException40 ("TimbreFiscalDigital not found.");
			}

			return new TimbreFiscalDigital {
				UUID = tfd.UUID,
				FechaTimbrado = tfd.FechaTimbrado,
				SelloCFD = tfd.SelloCFD,
				NoCertificadoSAT = tfd.NoCertificadoSAT,
				SelloSAT = tfd.SelloSAT,
				Leyenda = tfd.Leyenda,
				RfcProvCertif = tfd.RfcProvCertif
			};
		}

		public TimbreFiscalDigital GetStamp (string issuer, string uuid)
		{
			string xml_response = null;
			TimbreFiscalDigital tfd = null;

			using (var ws = new WSTimbradoSOAPClient (binding, address)) {
				var response = ws.RecuperarXML (Username, Password, uuid.ToUpper ());
				string err_number = response.codigo;
				string err_description = response.mensaje;

				if (err_number != "100") {
					throw new DFactureClientException40 (err_number, err_description);
				}

				xml_response = Encoding.UTF8.GetString (Convert.FromBase64String (response.xml));
			}

			var cfd = Comprobante.FromXml (xml_response);

			foreach (var item in cfd.Complemento) {
				if (item is TimbreFiscalDigital) {
					tfd = item as TimbreFiscalDigital;
					break;
				}
			}

			if (tfd == null) {
				throw new DFactureClientException40 ("TimbreFiscalDigital not found.");
			}

			return new TimbreFiscalDigital {
				UUID = tfd.UUID,
				FechaTimbrado = tfd.FechaTimbrado,
				SelloCFD = tfd.SelloCFD,
				NoCertificadoSAT = tfd.NoCertificadoSAT,
				SelloSAT = tfd.SelloSAT,
				Leyenda = tfd.Leyenda,
				RfcProvCertif = tfd.RfcProvCertif
			};
		}

		public bool Cancel (string issuer, string recipient, string uuid, string total, string cert, string privKey, string privKeyPassword, string reason, string uuidRelated)
		{
			using (var ws = new WSTimbradoSOAPClient (binding, address)) {
				var response = ws.CancelarCFDI (Username, Password, issuer, recipient, uuid.ToUpper (), total, cert, privKey, privKeyPassword, reason, uuidRelated);
				string err_number = response.codigo;
				string err_description = response.mensaje;

				if (err_number != "201" && err_number != "202" && err_number != "214") {
					throw new DFactureClientException40 (err_number, err_description);
				}
			}

			return true;
		}
	}
}

