//
// TestProgram.cs
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Mictlanix.CFDv33;
using Mictlanix.DFacture.Client;

namespace Tests {
	public class TestProgram {
		const string CSD_CERTIFICATE_FILE = "CSD_H&E951128469.cer";
		const string CSD_PRIVATE_KEY_FILE = "CSD_H&E951128469.key";
		const string CSD_PRIVATE_KEY_PWD = "12345678a";
		const string USERNAME = "DEMODaysoft";
		const string PASSWORD = "cfdi";

		static DateTime NOW = DateTime.Now;
		static DateTime TEST_DATE = new DateTime (NOW.Year, NOW.Month, NOW.Day,
							  NOW.Hour, NOW.Minute, NOW.Second, DateTimeKind.Unspecified);
		//static DateTime TEST_DATE = new DateTime (2016, 02, 09, 10, 11, 12, DateTimeKind.Unspecified);

		static void Main (string [] args)
		{
			StampTest ();
			//StampNominaTest ();
			//StampPagosTest ();
			//StampCreditNoteTest ();
			//GetStampTest ();
			//CancelTest ();
			//CancelAckTest ();
			//SaveIssuerTest ();
		}

		static void StampTest ()
		{
			var cfd = CreateCFD ();
			var cli = new DFactureClient (USERNAME, PASSWORD, DFactureClient.URL_TEST);
			var implocal = new ImpuestosLocalesRetencionesLocales {
				ImpLocRetenido = "5 A l millar",
				Importe = cfd.SubTotal * 0.005m,
				TasadeRetencion = 0.50m
			};

			AddItems (cfd, "Product", 3);

			//cfd.Complemento = new List<object> ();
			//cfd.Complemento.Add (new ImpuestosLocales {
			//	TotaldeRetenciones = 76.50m,
			//	RetencionesLocales = new ImpuestosLocalesRetencionesLocales [] {
			//		implocal
			//	}
			//});
			//cfd.Total -= implocal.Importe;

			cfd.Sign (File.ReadAllBytes (CSD_PRIVATE_KEY_FILE), Encoding.UTF8.GetBytes (CSD_PRIVATE_KEY_PWD));

			Console.WriteLine (cfd.ToXmlString ());

			var tfd = cli.Stamp (cfd);
			Console.WriteLine (tfd.ToXmlString ());
			Console.WriteLine (tfd.ToString ());

			cfd.Complemento.Add (tfd);

			Console.WriteLine (cfd.ToXmlString ());
			Console.WriteLine (cfd.ToString ());
		}

		static void StampNominaTest ()
		{
			var cfd = CreateNominaCFD ();
			var cli = new DFactureClient (USERNAME, PASSWORD, DFactureClient.URL_TEST);

			cfd.Sign (File.ReadAllBytes (CSD_PRIVATE_KEY_FILE), Encoding.UTF8.GetBytes (CSD_PRIVATE_KEY_PWD));

			File.WriteAllText ("nomina.xml", cfd.ToXmlString ());

			var tfd = cli.Stamp (cfd);
			Console.WriteLine (tfd.ToXmlString ());
			Console.WriteLine (tfd.ToString ());

			cfd.Complemento.Add (tfd);

			Console.WriteLine (cfd.ToXmlString ());
			Console.WriteLine (cfd.ToString ());
			File.WriteAllText ("nomina-signed.xml", cfd.ToXmlString ());
		}

		static void StampPagosTest ()
		{
			var cfd = CreatePagosCFD ();
			var cli = new DFactureClient (USERNAME, PASSWORD, DFactureClient.URL_TEST);

			cfd.Sign (File.ReadAllBytes (CSD_PRIVATE_KEY_FILE), Encoding.UTF8.GetBytes (CSD_PRIVATE_KEY_PWD));

			File.WriteAllText ("pagos.xml", cfd.ToXmlString ());

			var tfd = cli.Stamp (cfd);
			Console.WriteLine (tfd.ToXmlString ());
			Console.WriteLine (tfd.ToString ());

			cfd.Complemento.Add (tfd);

			Console.WriteLine (cfd.ToXmlString ());
			Console.WriteLine (cfd.ToString ());
			File.WriteAllText ("pagos-signed.xml", cfd.ToXmlString ());
		}

		static void StampCreditNoteTest ()
		{
			var cfd = CreateCreditNoteCFD ();
			var cli = new DFactureClient (USERNAME, PASSWORD, DFactureClient.URL_TEST);

			cfd.Sign (File.ReadAllBytes (CSD_PRIVATE_KEY_FILE), Encoding.UTF8.GetBytes (CSD_PRIVATE_KEY_PWD));

			var tfd = cli.Stamp (cfd);
			Console.WriteLine (tfd.ToXmlString ());
			Console.WriteLine (tfd);

			cfd.Complemento = new List<object> ();
			cfd.Complemento.Add (tfd);

			Console.WriteLine (cfd.ToXmlString ());
			Console.WriteLine (cfd);
		}

		static void GetStampTest ()
		{
			var cli = new DFactureClient (USERNAME, PASSWORD, DFactureClient.URL_TEST);
			var tfd = cli.GetStamp ("H&E951128469", "25D0B1B7-2FAE-4E92-BDE1-CF26AA1FBA22");

			Console.WriteLine (tfd.ToString ());
			Console.WriteLine (tfd.ToXmlString ());
		}

		static void CancelTest ()
		{
			var cli = new DFactureClient (USERNAME, PASSWORD, DFactureClient.URL_TEST);
			var ret = cli.Cancel ("H&E951128469", "XAXX010101000", "25D0B1B7-2FAE-4E92-BDE1-CF26AA1FBA22", "81.20",
					      Convert.ToBase64String (File.ReadAllBytes (CSD_CERTIFICATE_FILE)),
					      Convert.ToBase64String (File.ReadAllBytes (CSD_PRIVATE_KEY_FILE)),
						CSD_PRIVATE_KEY_PWD);

			Console.WriteLine ("Cancel: {0}", ret);
		}

		#region Helper Functions

		static Comprobante CreateCFD ()
		{
			var cfd = new Comprobante {
				TipoDeComprobante = c_TipoDeComprobante.Ingreso,
				Serie = "A",
				Folio = "1",
				Fecha = TEST_DATE,
				LugarExpedicion = "03810", // código postal
				MetodoPago = c_MetodoPago.PagoEnUnaSolaExhibicion,
				MetodoPagoSpecified = true,
				FormaPago = c_FormaPago.Efectivo,
				FormaPagoSpecified = true,
				TipoCambio = 1m,
				TipoCambioSpecified = true,
				Moneda = "MXN",
				NoCertificado = "30001000000400002463",
				Certificado = Convert.ToBase64String (File.ReadAllBytes (CSD_CERTIFICATE_FILE)),
				Emisor = new ComprobanteEmisor {
					Rfc = "H&E951128469",
					Nombre = "HERRERIA & ELECTRICOS S DE CV",
					RegimenFiscal = c_RegimenFiscal.GeneralDeLeyPersonasMorales,
				},
				Receptor = new ComprobanteReceptor {
					Rfc = "XAXX010101000",
					Nombre = "DEMO COMPANY SC",
					UsoCFDI = c_UsoCFDI.AdquisicionDeMercancias
				},
				Impuestos = new ComprobanteImpuestos (),
			};

			return cfd;
		}

		static void AddItem (Comprobante cfd, string code, string name, decimal qty, decimal amount)
		{
			int count = 1;

			if (cfd.Conceptos == null) {
				cfd.Conceptos = new ComprobanteConcepto [count];
			} else {
				count = cfd.Conceptos.Length + 1;
				var items = cfd.Conceptos;
				Array.Resize (ref items, count);
				cfd.Conceptos = items;
			}

			cfd.Conceptos [count - 1] = new ComprobanteConcepto {
				Cantidad = qty,
				ClaveUnidad = "H87",
				Unidad = "Pieza",
				NoIdentificacion = code,
				ClaveProdServ = "52161500",
				Descripcion = name,
				ValorUnitario = amount,
				Importe = Math.Round (qty * amount, 6),
				Impuestos = new ComprobanteConceptoImpuestos {
					Traslados = new ComprobanteConceptoImpuestosTraslado [] {
						new ComprobanteConceptoImpuestosTraslado {
							Impuesto = c_Impuesto.IVA,
							TipoFactor = c_TipoFactor.Tasa,
							Base = Math.Round(qty * amount, 6),
							Importe = Math.Round(qty * amount * 0.16m, 6),
							ImporteSpecified = true,
							TasaOCuota = 0.160000m,
							TasaOCuotaSpecified = true
						}
					}
				}
			};

			cfd.SubTotal = cfd.Conceptos.Sum (x => x.Importe);
			cfd.Total = Math.Round (cfd.SubTotal * 1.16m, 6);

			cfd.Impuestos.TotalImpuestosTrasladados = cfd.Total - cfd.SubTotal;
			cfd.Impuestos.TotalImpuestosTrasladadosSpecified = true;
			cfd.Impuestos.Traslados = new ComprobanteImpuestosTraslado [] {
				new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					Importe = cfd.Total - cfd.SubTotal,
					TasaOCuota = 0.160000m
				}
			};
		}

		static void AddItems (Comprobante cfd, string prefix, int count)
		{
			var sum = 0m;

			cfd.Conceptos = new ComprobanteConcepto [count];

			for (int i = 1; i <= count; i++) {
				cfd.Conceptos [i - 1] = new ComprobanteConcepto {
					Cantidad = i,
					ClaveUnidad = "H87",
					Unidad = "Pieza",
					NoIdentificacion = string.Format ("P{0:D4}", i),
					ClaveProdServ = "52161500",
					Descripcion = string.Format ("{0} {1:D4}", prefix, i),
					ValorUnitario = 5m * i,
					Importe = 5m * i * i,
					Impuestos = new ComprobanteConceptoImpuestos {
						Traslados = new ComprobanteConceptoImpuestosTraslado [] {
							new ComprobanteConceptoImpuestosTraslado {
								Impuesto = c_Impuesto.IVA,
								TipoFactor = c_TipoFactor.Tasa,
								Base = Math.Round(5m * i * i, 6),
								Importe = Math.Round(5m * i * i * 0.16m, 6),
								ImporteSpecified = true,
								TasaOCuota = 0.160000m,
								TasaOCuotaSpecified = true
							}
						}
					}
				};
				sum += 5m * i * i;
			}

			cfd.SubTotal = sum * 1.00m;
			cfd.Total = Math.Round (sum * 1.16m, 6);

			cfd.Impuestos.TotalImpuestosTrasladados = cfd.Total - cfd.SubTotal;
			cfd.Impuestos.TotalImpuestosTrasladadosSpecified = true;
			cfd.Impuestos.Traslados = new ComprobanteImpuestosTraslado [] {
				new ComprobanteImpuestosTraslado {
					Impuesto = c_Impuesto.IVA,
					TipoFactor = c_TipoFactor.Tasa,
					Importe = cfd.Total - cfd.SubTotal,
					TasaOCuota = 0.160000m
				}
			};
		}

		static Comprobante CreateNominaCFD ()
		{
			var nomina = new Nomina {
				TipoNomina = c_TipoNomina.Ordinaria,
				FechaPago = DateTime.Today,
				FechaInicialPago = DateTime.Today.AddDays (-8),
				FechaFinalPago = DateTime.Today.AddDays (-1),
				NumDiasPagados = 7,
				//TotalPercepciones = Percepciones.TotalSueldos + Percepciones.TotalSeparacionIndemnizacion + Percepciones.TotalJubilacionPensionRetiro,
				//TotalPercepcionesSpecified = true,
				//TotalDeducciones = Deducciones.TotalOtrasDeducciones + Deducciones.TotalImpuestosRetenidos,
				//TotalDeduccionesSpecified = true,
				//TotalOtrosPagos = 0m,
				//TotalOtrosPagosSpecified = true,
				Emisor = new NominaEmisor {
					//Curp = "", // CURP del patrón en caso de persona física
					//RegistroPatronal = "",
					//RegistroPatronal = "B5510768108",
					//RfcPatronOrigen = "", // RFC del patrón original en caso de jubilación o pensión
					//EntidadSNCF = new NominaEmisorEntidadSNCF () // entidades federativas, municipios, entidades paraestatales y paramunicipales.
				},
				Receptor = new NominaReceptor {
					Curp = "XEXX010101HNEXXXA4",
					NumSeguridadSocial = "123456789",
					//FechaInicioRelLaboral = DateTime.Today.AddYears (-1),
					//Antiguedad = "",
					TipoContrato = c_TipoContrato.ModalidadesDeContratacionDondeNoExisteRelacionDeTrabajo,
					Sindicalizado = NominaReceptorSindicalizado.No,
					TipoJornada = c_TipoJornada.Diurna,
					TipoJornadaSpecified = true,
					TipoRegimen = c_TipoRegimen.AsimiladosOtros,
					NumEmpleado = "1",
					Departamento = "ADMON",
					//Puesto = "",
					//RiesgoPuesto = c_RiesgoPuesto.ClaseI,
					PeriodicidadPago = c_PeriodicidadPago.Semanal,
					Banco = c_Banco.BANORTE_IXE,
					//CuentaBancaria = "",
					//SalarioBaseCotApor = 0m,
					//SalarioDiarioIntegrado = 0m,
					ClaveEntFed = c_Estado.MEX,
					//SubContratacion = new NominaReceptorSubContratacion [] {
					//	new NominaReceptorSubContratacion {
					//		RfcLabora = "",
					//		PorcentajeTiempo = 1.0m
					//	}
					//}
				},
				Percepciones = new NominaPercepciones {
					TotalSueldos = 4046.52m,
					TotalSueldosSpecified = true,
					TotalSeparacionIndemnizacion = 0m,
					TotalSeparacionIndemnizacionSpecified = true,
					TotalJubilacionPensionRetiro = 0m,
					TotalJubilacionPensionRetiroSpecified = true,
					TotalGravado = 4046.52m,
					TotalExento = 0m,
					Percepcion = new NominaPercepcionesPercepcion [] {
						new NominaPercepcionesPercepcion {
							TipoPercepcion = c_TipoPercepcion.IngresosAsimiladosASalarios,
							Clave = "P-001",
							Concepto = "SUELDOS",
							ImporteGravado = 4046.52m,
							ImporteExento = 0m,
							//AccionesOTitulos = new NominaPercepcionesPercepcionAccionesOTitulos ()
							HorasExtra = new NominaPercepcionesPercepcionHorasExtra [] {
								//new NominaPercepcionesPercepcionHorasExtra {
								//	Dias = 0,
								//	TipoHoras = c_TipoHoras.Simples,
								//	HorasExtra = 0,
								//	ImportePagado = 0
								//}
							}
						}
					},
					//JubilacionPensionRetiro = new NominaPercepcionesJubilacionPensionRetiro {
					//	//TotalUnaExhibicion = 0m,
					//	//TotalParcialidad = 0m,
					//	//MontoDiario = 0m,
					//	IngresoAcumulable = 0m,
					//	IngresoNoAcumulable = 0m,
					//},
					SeparacionIndemnizacion = new NominaPercepcionesSeparacionIndemnizacion {
						TotalPagado = 0m,
						NumAnosServicio = 0,
						UltimoSueldoMensOrd = 0m,
						IngresoAcumulable = 0m,
						IngresoNoAcumulable = 0m
					}
				},
				Deducciones = new NominaDeducciones {
					//TotalOtrasDeducciones = 0m,
					//TotalOtrasDeduccionesSpecified = true,
					TotalImpuestosRetenidos = 608.97m,
					TotalImpuestosRetenidosSpecified = true,
					Deduccion = new NominaDeduccionesDeduccion [] {
						new NominaDeduccionesDeduccion {
							TipoDeduccion = c_TipoDeduccion.ISR,
							Clave = "D-002",
							Concepto = "ISR",
							Importe = 608.97m
						}
					}
				},
				//OtrosPagos = new NominaOtroPago[] {
				//	//new NominaOtroPago {
				//	//	TipoOtroPago = c_TipoOtroPago.Viaticos,
				//	//	Clave = "00101",
				//	//	Concepto = "Viaticos",
				//	//	Importe = 0m,
				//	//	SubsidioAlEmpleo = new NominaOtroPagoSubsidioAlEmpleo {
				//	//		SubsidioCausado = 0m
				//	//	},
				//	//	CompensacionSaldosAFavor = new NominaOtroPagoCompensacionSaldosAFavor {
				//	//		SaldoAFavor = 0m,
				//	//		Ano = 0,
				//	//		RemanenteSalFav = 0m
				//	//	}
				//	//}
				//},
				//Incapacidades = new NominaIncapacidad [] {
				//	//new NominaIncapacidad {
				//	//	DiasIncapacidad = 0,
				//	//	TipoIncapacidad = c_TipoIncapacidad.EnfermedadEnGeneral,
				//	//	ImporteMonetario = 0m
				//	//}
				//}

			};

			nomina.TotalPercepciones = nomina.Percepciones.TotalSueldos + nomina.Percepciones.TotalSeparacionIndemnizacion + nomina.Percepciones.TotalJubilacionPensionRetiro;
			nomina.TotalPercepcionesSpecified = true;

			nomina.TotalDeducciones = nomina.Deducciones.TotalOtrasDeducciones + nomina.Deducciones.TotalImpuestosRetenidos;
			nomina.TotalDeduccionesSpecified = true;

			if (nomina.OtrosPagos != null && nomina.OtrosPagos.Any ()) {
				nomina.TotalOtrosPagos = nomina.OtrosPagos.Sum (x => x.Importe);
				nomina.TotalOtrosPagosSpecified = true;
			}

			var cfd = new Comprobante {
				Serie = "N",
				Folio = "1",
				Fecha = TEST_DATE,
				FormaPago = c_FormaPago.PorDefinir,
				FormaPagoSpecified = true,
				NoCertificado = "20001000000200001428",
				Certificado = Convert.ToBase64String (File.ReadAllBytes (CSD_CERTIFICATE_FILE)),
				SubTotal = nomina.TotalPercepciones + nomina.TotalOtrosPagos,
				Descuento = nomina.TotalDeducciones,
				DescuentoSpecified = true,
				Moneda = "MXN",
				Total = nomina.TotalPercepciones + nomina.TotalOtrosPagos - nomina.TotalDeducciones,
				TipoDeComprobante = c_TipoDeComprobante.Nomina,
				MetodoPago = c_MetodoPago.PagoEnUnaSolaExhibicion,
				LugarExpedicion = "03810",
				//Confirmacion = "",
				//CfdiRelacionados = new ComprobanteCfdiRelacionados (),
				Emisor = new ComprobanteEmisor {
					Rfc = "H&E951128469",
					Nombre = "HERRERIA & ELECTRICOS S DE CV",
					RegimenFiscal = c_RegimenFiscal.GeneralDeLeyPersonasMorales,
				},
				Receptor = new ComprobanteReceptor {
					Rfc = "XAXX010101000",
					Nombre = "DEMO COMPANY SC",
					UsoCFDI = c_UsoCFDI.PorDefinir
				},
				Conceptos = new ComprobanteConcepto [] {
					new ComprobanteConcepto {
						ClaveProdServ = "84111505",
						Cantidad = 1,
						ClaveUnidad = "ACT",
						Descripcion= "Pago de nómina",
						ValorUnitario = nomina.TotalPercepciones + nomina.TotalOtrosPagos,
						Importe = nomina.TotalPercepciones + nomina.TotalOtrosPagos,
						Descuento = nomina.TotalDeducciones,
						DescuentoSpecified = true
					}
				},
				Complemento = new List<object> ()
			};

			cfd.Complemento.Add (nomina);

			return cfd;
		}

		static Comprobante CreatePagosCFD ()
		{
			var pagos = new Pagos {
				Pago = new PagosPago [] {
					new PagosPago {
						FechaPago = TEST_DATE.AddDays(-3),
						FormaDePagoP = c_FormaPago.Efectivo,
						MonedaP = "MXN",
						//TipoCambioP = 1,
						//TipoCambioPSpecified = true,
						Monto = 10.00m,
						NumOperacion = "0000051",
						//RfcEmisorCtaOrd = "BNM840515VB1", // ProFact Error: El campo RfcEmisorCtaOrd no se debe registrar.
                        //RfcEmisorCtaOrd = "XEXX010101000",
                        NomBancoOrdExt = "Citibanamex",
						//CtaOrdenante = "123456789101112131", // ProFact Error: El campo CtaOrdenante no se debe registrar. 
						//RfcEmisorCtaBen = "BBA830831LJ2", // BBVA Bancomer (ProFact Error: El campo RfcEmisorCtaBen no se debe registrar. )
						//CtaBeneficiario = "123456789101114558", // ProFact Error: El campo CtaBeneficiario no se debe registrar. 
						//TipoCadPago = c_TipoCadenaPago.SPEI,
						//TipoCadPagoSpecified = true,
						//CertPago = Encoding.UTF8.GetBytes (""), // certificado que corresponde al pago, como una cadena de texto en formato base 64
						//CadPago = "||Pago|Banco|300.00||",      // cadena original del comprobante de pago generado por la entidad emisora de la cuenta beneficiaria
						//SelloPago = Encoding.UTF8.GetBytes (""),// sello digital que se asocie al pago
						DoctoRelacionado = new PagosPagoDoctoRelacionado [] {
							new PagosPagoDoctoRelacionado {
								IdDocumento = "B1930368-6194-447D-8F41-95FAF528E72B",
								Serie = "A",
								Folio = "1",
								MonedaDR = "MXN",
								TipoCambioDR = 1.0m,
								MetodoDePagoDR = c_MetodoPago.PagoEnParcialidadesODiferido,
								NumParcialidad = "1",
								ImpSaldoAnt = 81.20m,
								ImpSaldoAntSpecified = true,
								ImpPagado = 10.00m,
								ImpPagadoSpecified = true
							}
						}
					}
				}
			};

			foreach (var doc in pagos.Pago [0].DoctoRelacionado) {
				doc.ImpSaldoInsoluto = doc.ImpSaldoAnt - doc.ImpPagado;
				doc.ImpSaldoInsolutoSpecified = true;
			}

			var cfd = new Comprobante {
				Serie = "P",
				Folio = "1",
				Fecha = TEST_DATE,
				Sello = null,
				NoCertificado = "20001000000200001428",
				Certificado = Convert.ToBase64String (File.ReadAllBytes (CSD_CERTIFICATE_FILE)),
				SubTotal = 0,
				Moneda = "XXX",
				Total = 0,
				TipoDeComprobante = c_TipoDeComprobante.Pago,
				LugarExpedicion = "03810", // código postal
				//CfdiRelacionados = new ComprobanteCfdiRelacionados {
				//	TipoRelacion = c_TipoRelacion.Sustitucion,
				//	CfdiRelacionado = new ComprobanteCfdiRelacionadosCfdiRelacionado [] {
				//		new ComprobanteCfdiRelacionadosCfdiRelacionado {
				//			UUID = "B1930368-6194-447D-8F41-95FAF528E72B"
				//		}
				//	}
				//},
				Emisor = new ComprobanteEmisor {
					Rfc = "H&E951128469",
					Nombre = "HERRERIA & ELECTRICOS S DE CV",
					RegimenFiscal = c_RegimenFiscal.GeneralDeLeyPersonasMorales,
				},
				Receptor = new ComprobanteReceptor {
					Rfc = "XAXX010101000",
					Nombre = "DEMO COMPANY SC",
					UsoCFDI = c_UsoCFDI.PorDefinir
				},
				Conceptos = new ComprobanteConcepto [] {
					new ComprobanteConcepto {
						ClaveProdServ = "84111506",
						Cantidad = 1,
						ClaveUnidad = "ACT",
						Descripcion = "Pago",
						ValorUnitario = 0,
						Importe = 0
					}
				},
				Complemento = new List<object> ()
			};

			cfd.Complemento.Add (pagos);

			return cfd;
		}

		static Comprobante CreateCreditNoteCFD ()
		{
			var cfd = new Comprobante {
				TipoDeComprobante = c_TipoDeComprobante.Ingreso,
				Serie = "A",
				Folio = "1",
				Fecha = TEST_DATE,
				LugarExpedicion = "03810", // código postal
				MetodoPago = c_MetodoPago.PagoEnUnaSolaExhibicion,
				MetodoPagoSpecified = true,
				FormaPago = c_FormaPago.PorDefinir,
				FormaPagoSpecified = true,
				TipoCambio = 1m,
				TipoCambioSpecified = true,
				Moneda = "MXN",
				NoCertificado = "30001000000400002463",
				Certificado = Convert.ToBase64String (File.ReadAllBytes (CSD_CERTIFICATE_FILE)),
				Emisor = new ComprobanteEmisor {
					Rfc = "H&E951128469",
					Nombre = "HERRERIA & ELECTRICOS S DE CV",
					RegimenFiscal = c_RegimenFiscal.GeneralDeLeyPersonasMorales,
				},
				Receptor = new ComprobanteReceptor {
					Rfc = "XAXX010101000",
					Nombre = "DEMO COMPANY SC",
					UsoCFDI = c_UsoCFDI.AdquisicionDeMercancias
				},
				Conceptos = new ComprobanteConcepto [] {
					new ComprobanteConcepto {
						ClaveProdServ = "84111506",
						Cantidad = 1,
						ClaveUnidad = "ACT",
						Descripcion= "% del saldo de todos los CFDI relacionados",
						ValorUnitario = 100,
						Importe = 100
					}
				},
				CfdiRelacionados = new ComprobanteCfdiRelacionados {
					TipoRelacion = c_TipoRelacion.NotaDeCredito,
					CfdiRelacionado = new ComprobanteCfdiRelacionadosCfdiRelacionado [] {
						new ComprobanteCfdiRelacionadosCfdiRelacionado {
							UUID = "d291a911-6476-4c79-a9c8-0ccddfe00858"
						}
					}
				},
			};

			cfd.SubTotal = cfd.Conceptos.Sum (x => x.Importe);
			cfd.Total = cfd.Conceptos.Sum (x => x.Importe);

			return cfd;
		}

		#endregion
	}
}

