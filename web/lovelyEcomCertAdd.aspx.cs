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
    public partial class lovelyEcomCertAdd : System.Web.UI.Page
    {
        protected void Page_Load(object sender, System.EventArgs e)
        {
            Customer thisCustomer = ((AspDotNetStorefrontPrincipal)Context.User).ThisCustomer;
            AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud _tc = new AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud();
            string str = CommonLogic.FormCanBeDangerousContent("certificateID");

            AspDotNetStorefrontCore.net.taxcloud.api.ExemptionCertificate _certificate = new ExemptionCertificate();

            _certificate.Detail = new ExemptionCertificateDetail();
         
            _certificate.Detail.SinglePurchaseOrderNumber = CommonLogic.FormCanBeDangerousContent("SinglePurchaseOrderNumber");
            if(string.IsNullOrEmpty(_certificate.Detail.SinglePurchaseOrderNumber))
                 _certificate.Detail.SinglePurchase =false;
            else
                 _certificate.Detail.SinglePurchase =true;

            ExemptState[] exemptState = new ExemptState[1];
            exemptState[0] = new ExemptState();
            exemptState[0].StateAbbr = (AspDotNetStorefrontCore.net.taxcloud.api.State)(Enum.Parse(typeof(AspDotNetStorefrontCore.net.taxcloud.api.State), CommonLogic.Form("ExemptState"), true));
            //exemptState[0].ReasonForExemption = CommonLogic.FormCanBeDangerousContent("ReasonForExemption");
            //exemptState[0].IdentificationNumber = CommonLogic.FormCanBeDangerousContent("IdentificationNumber");
            _certificate.Detail.ExemptStates = exemptState;
            _certificate.Detail.PurchaserTaxID = new TaxID();
            _certificate.Detail.PurchaserTaxID.TaxType = (TaxIDType)(Enum.Parse(typeof(TaxIDType), CommonLogic.Form("TaxType"), true));
            _certificate.Detail.PurchaserTaxID.IDNumber = CommonLogic.FormCanBeDangerousContent("IDNumber"); ; ;
            _certificate.Detail.PurchaserFirstName = CommonLogic.FormCanBeDangerousContent("PurchaserFirstName"); ; ;
            _certificate.Detail.PurchaserLastName = CommonLogic.FormCanBeDangerousContent("PurchaserLastName"); ; ;
            _certificate.Detail.PurchaserAddress1 = CommonLogic.FormCanBeDangerousContent("PurchaserAddress1"); ; ;
            _certificate.Detail.PurchaserCity = CommonLogic.FormCanBeDangerousContent("PurchaserCity"); ; ;
            _certificate.Detail.PurchaserState = (AspDotNetStorefrontCore.net.taxcloud.api.State)(Enum.Parse(typeof(AspDotNetStorefrontCore.net.taxcloud.api.State),CommonLogic.FormCanBeDangerousContent("PurchaserState"),true));
            _certificate.Detail.PurchaserZip = CommonLogic.FormCanBeDangerousContent("PurchaserZip");  
            _certificate.Detail.PurchaserBusinessType = (BusinessType)(Enum.Parse(typeof(BusinessType), CommonLogic.FormCanBeDangerousContent("PurchaserBusinessType"), true));
            _certificate.Detail.PurchaserExemptionReason = (ExemptionReason)(Enum.Parse(typeof(ExemptionReason),CommonLogic.FormCanBeDangerousContent("PurchaserExemptionReason"),true));
            _certificate.Detail.PurchaserExemptionReasonValue = CommonLogic.FormCanBeDangerousContent("PurchaserExemptionReasonValue"); ; ;

            AddCertificateRsp addRs = _tc.AddExemptCertificate(AppLogic.AppConfig("taxcloud.apiloginid"), AppLogic.AppConfig("taxcloud.apikey"), thisCustomer.CustomerID.ToString(), _certificate);
            if(addRs.ResponseType!= MessageType.Error)
                DB.ExecuteSQL("update shoppingcart set certificateID=" + DB.SQuote(addRs.CertificateID) + " where CustomerID=" + thisCustomer.CustomerID);
            
        }
        
    }
}
