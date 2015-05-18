// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontCore.net.taxcloud.api;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Collections;


namespace AspDotNetStorefront
{
    /// <summary>
    /// Summary description for receipt.
    /// </summary>
    public partial class lovelyEcomCertlist : System.Web.UI.Page
    {
        protected void Page_Load(object sender, System.EventArgs e)
        {
            Customer thisCustomer = ((AspDotNetStorefrontPrincipal)Context.User).ThisCustomer;
            AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud _tc = new AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud();
            GetCertificatesRsp rsp = _tc.GetExemptCertificates(AppLogic.AppConfig("taxcloud.apiloginid"), AppLogic.AppConfig("taxcloud.apikey"), thisCustomer.CustomerID.ToString());

            //TaxcloudCertificates tc = new TaxcloudCertificates();
            //tc.NOTICE = "THIS JSONP FEED IS INTENDED FOR TAXCLOUD METCHANTS ONLY.";
            //tc.COPYRIGHT = "COPYRIGHT 2011 FEDTAX";
            //tc.LICENSE = @"USE GOVERNED BY THE TAXCLOUD TERMS OF SERVICE (https://taxloud.net/tos\/)";
            //tc.cert_list = new List<ExemptionCertificate>();
            StringBuilder sb = new StringBuilder();
            sb.Append("");
            sb.Append(" taxcloudCertificates({");
            sb.Append("\"NOTICE\": \"THIS JSONP FEED IS INTENDED FOR TAXCLOUD METCHANTS ONLY.\",");
            sb.Append(" \"COPYRIGHT\": \"COPYRIGHT 2011 FEDTAX\",");
            sb.Append(" \"LICENSE\": \"USE GOVERNED BY THE TAXCLOUD TERMS OF SERVICE ()\",");
            sb.Append(" \"cert_list\": [");
            //sb.Append("    {");
            //sb.AppendFormat("        \"CertificateID\": \"{0}\",", "IDDDD");
            //sb.Append("       \"ExemptionCertificateDetail\": {");
            //sb.Append("          \"ArrayOfExemptStates\": [");
            //sb.Append("                {");
            //sb.Append("                    \"ExemptState\": \"AL\"");
            //sb.Append("             },");
            //sb.Append("             {");
            //sb.Append("               \"ExemptState\": \"FL\"");
            //sb.Append("          }");
            //sb.Append("       ],");
            //sb.Append("      \"SinglePurchase\": \"false\",");
            //sb.Append("       \"SinglePurchaseOrderNumber\": \"\",");
            //sb.Append("       \"DateEntered\": \"January 31, 2011\",");
            //sb.Append("        \"PurchaserFirstName\": \"David2\",");
            //sb.Append("        \"PurchaserLastName\": \"Campbell2\",");
            //sb.Append("       \"PurchaserAddress1\": \"162 East Avenue\",");
            //sb.Append("       \"PurchaserAddress2\": \"\",");
            //sb.Append("       \"PurchaserCity\": \"Norwalk\",");
            //sb.Append("       \"PurchaserState\": \"CT\",");
            //sb.Append("       \"PurchaserZip\": \"06851\",");
            //sb.Append("       \"TaxIDType\": \"FEIN\",");
            //sb.Append("       \"PurchaserTaxID\": \"**-****789\",");
            //sb.Append("       \"PurchaserBusinessType\": \"AccommodationAndFoodServices\",");
            //sb.Append("       \"PurchaserBusinessTypeOtherValue\": \"\",");
            //sb.Append("       \"PurchaserExemptionReason\": \"FederalGovernmentDepartment\",");
            //sb.Append("       \"PurchaserExemptionReasonValue\": \"FedGov ID\"");
            //sb.Append("   }");
            //sb.Append("  },");

            StringBuilder strExemptionCertificate = new StringBuilder();
            foreach(AspDotNetStorefrontCore.net.taxcloud.api.ExemptionCertificate _certificate in rsp.ExemptCertificates)
            {
                strExemptionCertificate.Append("  {");
                strExemptionCertificate.AppendFormat("    \"CertificateID\": \"{0}\",", _certificate.CertificateID);
                strExemptionCertificate.Append("    \"ExemptionCertificateDetail\": {");
                strExemptionCertificate.Append("        \"ArrayOfExemptStates\": [");
             
                StringBuilder strExemptStates = new StringBuilder();
                foreach (AspDotNetStorefrontCore.net.taxcloud.api.ExemptState _state in _certificate.Detail.ExemptStates)
                {
                    strExemptStates.Append("           {");
                    strExemptStates.AppendFormat("               \"ExemptState\": \"{0}\"",_state.StateAbbr);
                    strExemptStates.Append("           },");
                }
                strExemptionCertificate.Append(strExemptStates.ToString().TrimEnd(','));
             
                strExemptionCertificate.Append("       ],");
                strExemptionCertificate.AppendFormat("       \"SinglePurchase\": \"{0}\",", _certificate.Detail.SinglePurchase.ToString());
                strExemptionCertificate.AppendFormat("       \"SinglePurchaseOrderNumber\": \"{0}\",",_certificate.Detail.SinglePurchaseOrderNumber);
                strExemptionCertificate.AppendFormat("       \"DateEntered\": \"{0}\",", _certificate.Detail.CreatedDate.ToShortDateString());
                strExemptionCertificate.AppendFormat("       \"PurchaserFirstName\": \"{0}\",",_certificate.Detail.PurchaserFirstName);
                strExemptionCertificate.AppendFormat("       \"PurchaserLastName\": \"{0}\",", _certificate.Detail.PurchaserLastName);
                strExemptionCertificate.AppendFormat("      \"PurchaserAddress1\": \"{0}\",", _certificate.Detail.PurchaserAddress1);
                strExemptionCertificate.AppendFormat("       \"PurchaserAddress2\": \"{0}\",", _certificate.Detail.PurchaserAddress2);
                strExemptionCertificate.AppendFormat("       \"PurchaserCity\": \"{0}\",", _certificate.Detail.PurchaserCity);
                strExemptionCertificate.AppendFormat("       \"PurchaserState\": \"{0}\",", _certificate.Detail.PurchaserState);
                strExemptionCertificate.AppendFormat("       \"PurchaserZip\": \"{0}\",", _certificate.Detail.PurchaserZip);
                strExemptionCertificate.AppendFormat("       \"PurchaserTaxID\": \"{0}\",",_certificate.Detail.PurchaserTaxID.IDNumber);
                strExemptionCertificate.AppendFormat("       \"PurchaserBusinessType\": \"{0}\",",_certificate.Detail.PurchaserBusinessType.ToString());
                strExemptionCertificate.AppendFormat("       \"PurchaserBusinessTypeOtherValue\": \"{0}\",",_certificate.Detail.PurchaserBusinessTypeOtherValue==null? "":_certificate.Detail.PurchaserBusinessTypeOtherValue.ToString());
                strExemptionCertificate.AppendFormat("       \"PurchaserExemptionReason\": \"{0}\",",_certificate.Detail.PurchaserExemptionReason.ToString());
                strExemptionCertificate.AppendFormat("       \"PurchaserExemptionReasonValue\": \"{0}\"",_certificate.Detail.PurchaserExemptionReasonValue.ToString());
                strExemptionCertificate.Append("   }");
                strExemptionCertificate.Append(" },");
            }
            sb.Append(  strExemptionCertificate.ToString().TrimEnd(','));
            sb.Append("  ]");
 
            sb.Append("})");

            Response.Write(sb.ToString());
            return;

            #region debug
            string str1 = "";
            //str1 += "<br/>"; str1 += "<br/>";
            str1 += "taxcloudCertificates({ \"NOTICE\" : \"THIS JSONP FEED IS INTENDED FOR TAXCLOUD METCHANTS ONLY.\", \"COPYRIGHT\" : \"COPYRIGHT 2011 FEDTAX\", \"LICENSE\" : \"USE GOVERNED BY THE TAXCLOUD TERMS OF SERVICE ( )\", \"cert_list\":[{ \"CertificateID\":\"b7fd09ec-2c9f-4613-91b7-d1668c0aa72a\", \"ExemptionCertificateDetail\":{ \"ArrayOfExemptStates\":[ {\"ExemptState\":\"AL\"}, {\"ExemptState\":\"AR\"}, {\"ExemptState\":\"GA\"}, {\"ExemptState\":\"TX\"}, {\"ExemptState\":\"MN\"}, {\"ExemptState\":\"MS\"}, {\"ExemptState\":\"MO\"}, {\"ExemptState\":\"FL\"} ], \"SinglePurchase\":\"false\", \"SinglePurchaseOrderNumber\":\"\", \"DateEntered\":\"January 31, 2011\", \"PurchaserFirstName\":\"David\", \"PurchaserLastName\":\"Campbell\", \"PurchaserAddress1\":\"162 East Avenue\", \"PurchaserAddress2\":\"\", \"PurchaserCity\":\"Norwalk\", \"PurchaserState\":\"CT\", \"PurchaserZip\":\"06851\", \"TaxIDType\":\"FEIN\", \"PurchaserTaxID\":\"**-****789\", \"PurchaserBusinessType\":\"AccommodationAndFoodServices\", \"PurchaserBusinessTypeOtherValue\":\"\", \"PurchaserExemptionReason\":\"FederalGovernmentDepartment\", \"PurchaserExemptionReasonValue\":\"FedGov ID\" }}, {\"CertificateID\":\"00022\", \"ExemptionCertificateDetail\":{ \"ArrayOfExemptStates\":[ {\"ExemptState\":\"WA\"} ], \"CertificateID\":\"00001\", \"SinglePurchase\":\"true\", \"SinglePurchaseOrderNumber\":\"66556\", \"DateEntered\":\"January 31, 2011\", \"PurchaserFirstName\":\"R. David L.\", \"PurchaserLastName\":\"Campbell\", \"PurchaserAddress1\":\"3205 South Judkins\", \"PurchaserAddress2\":\"\", \"PurchaserCity\":\"Seattle\", \"PurchaserState\":\"WA\", \"PurchaserZip\":\"98144\", \"PurchaserTaxID\":\"***-**-9012\", \"PurchaserBusinessType\":\"Other\", \"PurchaserBusinessTypeOtherValue\":\"Internet Sales Tax Prep\", \"PurchaserExemptionReason\":\"Industrial Production Or Manufacturing\", \"PurchaserExemptionReasonValue\":\"Widgets\" }} ] })";


            //tt.cert_list = new List<ExemptionCertificate>();
            //ExemptionCertificate entity = new ExemptionCertificate();
            //entity.CertificateID = new Guid("b7fd09ec-2c9f-4613-91b7-d1668c0aa72a");
            //entity.ExemptionCertificateDetail = new List<ExemptionCertificateDetail>();
            //entity.ExemptionCertificateDetail

            Response.Write(str1);
            //Response.Write("<br/>");
            //Response.Write("XXXXXXXXX<br/>XXXXXXXXXXXXXXXXXX");
            //Response.Write(str2);
            #endregion
        }
    }
}
