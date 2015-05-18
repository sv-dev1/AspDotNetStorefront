// --------------------------------------------------------------------------------
// Copyright lovelyEcommerce.com. All Rights Reserved.
// http://www.lovelyEcommerce.com
// For details on this license please visit the homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Xml;
using System.Reflection;
using AspDotNetStorefrontCore;

namespace AspDotNetStorefront
{
    /// <summary>
    /// Summary description for AddressValidation
    /// </summary>
    public partial class AddressValidation
    {

        /// <summary>
        /// Validate Address using US Postal Service API.
        /// </summary>
        /// <param name="EnteredAddress">The address as entered by a customer</param>
        /// <param name="ResultAddress">The resulting validated address</param>
        /// <returns>String,
        /// ro_OK => ResultAddress = EnteredAddress proceed with no further user review,
        /// 'some message' => address requires edit or verification by customer
        /// </returns>
        public String taxcloudValidate(Address EnteredAddress, out Address ResultAddress)
        {
            string result = AppLogic.ro_OK;
            ResultAddress = new Address();
            string uspsUserID = AppLogic.AppConfig("VerifyAddressesProvider.USPS.UserID");
            string Address1 = EnteredAddress.Address1;
            string Address2 = EnteredAddress.Address2;
            string City = EnteredAddress.City;
            string State = EnteredAddress.State;
            string Zip5 = EnteredAddress.Zip;
            string Zip4 = string.Empty;

            AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud tcaddress = new AspDotNetStorefrontCore.net.taxcloud.api.TaxCloud();
            AspDotNetStorefrontCore.net.taxcloud.api.VerifiedAddress verifiedaddress = tcaddress.VerifyAddress(uspsUserID, Address1, Address2, City, State, Zip5, Zip4);
            if (Localization.ParseNativeInt(verifiedaddress.ErrNumber) == 0)
            {
               
                ResultAddress.Address1  = verifiedaddress.Address1;
                ResultAddress.Address2 = verifiedaddress.Address2;
                ResultAddress.City = verifiedaddress.City;
                ResultAddress.State = verifiedaddress.State;
                ResultAddress.Zip = verifiedaddress.Zip5;
 
                result= AppLogic.ro_OK;
            }
            else
            {
                result = string.Format("Warning: your address {0} {1} {2} {3} can not be verified!( {4} )", EnteredAddress.Address1, EnteredAddress.City, EnteredAddress.State, EnteredAddress.Zip, verifiedaddress.ErrNumber + " " + verifiedaddress.ErrDescription);
            }
            return result;
        }
    }
}
