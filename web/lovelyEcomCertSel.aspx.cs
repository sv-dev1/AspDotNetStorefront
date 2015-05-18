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
    public partial class lovelyEcomCertSel : System.Web.UI.Page
    {
        protected void Page_Load(object sender, System.EventArgs e)
        {
            Customer thisCustomer = ((AspDotNetStorefrontPrincipal)Context.User).ThisCustomer;
            AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud _tc = new AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud();
            string strCertificateID = CommonLogic.FormCanBeDangerousContent("certificateID");
            if(!String.IsNullOrEmpty(strCertificateID))
              DB.ExecuteSQL("update shoppingcart set certificateID=" + DB.SQuote(strCertificateID) + " where CustomerID=" + thisCustomer.CustomerID);
            
            //_tc.DeleteExemptCertificate(AppLogic.AppConfig("taxcloud.apiloginid"), AppLogic.AppConfig("taxcloud.apikey"), strCertificateID);
        }
    }
}
