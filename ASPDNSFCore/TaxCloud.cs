// --------------------------------------------------------------------------------
// Copyright LovelyEcommerce.com All Rights Reserved.
// www.LovelyEcommerce.com
// For details on this license please visit the offical site at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AspDotNetStorefront.AddInConfiguration;

using AspDotNetStorefrontCore.net.taxcloud.api;
using System.Data.SqlClient;
using System.Data;

namespace AspDotNetStorefrontCore
{
	public class TaxCloud
	{

		#region AppConfig Keys

    
        const string ApiAppConfigID = "taxcloud.apiloginid";
        const string ApiAppConfigKey = "taxcloud.apikey";
        const string ApiAppConfigShippingTaxClassCode = "taxcloud.ShippingTaxClassCode";

        const string DeliveredBySellerAppConfig = "taxcloud.deliveredBySeller";
        const string AppConfigOriginAddress = "RTShipping.OriginAddress";
        const string AppConfigOriginAddress2 = "RTShipping.OriginAddress2";
        const string AppConfigOriginCity = "RTShipping.OriginCity";
        const string AppConfigOriginState = "RTShipping.OriginState";
        const string AppConfigOriginZip = "RTShipping.OriginZip";

        

        const string AppConfigUSPSUserID = "VerifyAddressesProvider.USPS.UserID";
        
		#endregion

		#region Predefined SKU's

        const string ShippingItemSku = "taxcloud Shipping Item";
        const string OrderOptionItemSku = "taxcloud Order Option ";

		#endregion

      

        public bool Enabled { get; protected set; }
        public String ApiLoginID { get; protected set; }
        public String ApiKey { get; protected set; }
        public int APiShippingTaxClassCode { get; protected set; }
        public Boolean DeliveredBySeller { get; protected set; }

        public String OriginAddress { get; protected set; }
        public String OriginAddress2 { get; protected set; }
        public String OriginCity { get; protected set; }
        public String OriginState { get; protected set; }
        public String OriginZip { get; protected set; }

        public String USPSUserID { get; protected set; }

        private net.taxcloud.api.TaxCloud tc;

        //private Address originAddress;
        private net.taxcloud.api.Address refOrigin;
        public TaxCloud()
		{
			ValidateAppConfigs();
			LoadConfiguration();
            tc = new net.taxcloud.api.TaxCloud();
            refOrigin = ConvertAddress(GetOriginAddress());
		}

		#region Configuration

       
        protected void ValidateAppConfigs()
		{
           
            if (String.IsNullOrEmpty(AppLogic.AppConfig(ApiAppConfigID)))
                throw new Exception(String.Format("Please ensure that you have configured a value for the \"{0}\" AppConfig.", ApiAppConfigID));

            if (String.IsNullOrEmpty(AppLogic.AppConfig(ApiAppConfigKey)))
                throw new Exception(String.Format("Please ensure that you have configured a value for the \"{0}\" AppConfig.", ApiAppConfigKey));

            if (String.IsNullOrEmpty(AppLogic.AppConfig(AppConfigOriginAddress)))
                throw new Exception(String.Format("Please ensure that you have configured a value for the \"{0}\" AppConfig.", AppConfigOriginAddress));

            if (String.IsNullOrEmpty(AppLogic.AppConfig(AppConfigOriginCity)))
                throw new Exception(String.Format("Please ensure that you have configured a value for the \"{0}\" AppConfig.", AppConfigOriginCity));

            if (String.IsNullOrEmpty(AppLogic.AppConfig(AppConfigOriginState)))
                throw new Exception(String.Format("Please ensure that you have configured a value for the \"{0}\" AppConfig.", AppConfigOriginState));

            if (String.IsNullOrEmpty(AppLogic.AppConfig(AppConfigOriginZip)))
                throw new Exception(String.Format("Please ensure that you have configured a value for the \"{0}\" AppConfig.", AppConfigOriginZip));

            if (String.IsNullOrEmpty(AppLogic.AppConfig(AppConfigUSPSUserID)))
                throw new Exception(String.Format("Please ensure that you have configured a value for the \"{0}\" AppConfig.", AppConfigUSPSUserID)); 
		
        }

		protected void LoadConfiguration()
		{
           
            ApiLoginID = AppLogic.AppConfig(ApiAppConfigID);
            ApiKey = AppLogic.AppConfig(ApiAppConfigKey);
            DeliveredBySeller = AppLogic.AppConfigBool(DeliveredBySellerAppConfig);
            APiShippingTaxClassCode = AppLogic.AppConfigUSInt(ApiAppConfigShippingTaxClassCode) == 0 ? 1101 : AppLogic.AppConfigUSInt(ApiAppConfigShippingTaxClassCode);

            OriginAddress = AppLogic.AppConfig(AppConfigOriginAddress);
            OriginAddress2 = AppLogic.AppConfig(AppConfigOriginAddress2);
            OriginCity = AppLogic.AppConfig(AppConfigOriginCity);
            OriginState = AppLogic.AppConfig(AppConfigOriginState);
            OriginZip = AppLogic.AppConfig(AppConfigOriginZip);
            USPSUserID = AppLogic.AppConfig(AppConfigUSPSUserID);
           
		}

		#endregion

        #region Taxcloud Address

        private Address GetOriginAddress()
        {
            return new Address
            {
                Address1 = OriginAddress,
                Address2 = OriginAddress2,
                City = OriginCity,
                State = OriginState,
                Zip = OriginZip
            };
        }

        private net.taxcloud.api.Address ConvertAddress(int adnsfAddressId)
        {
            var adnsfAddress = new Address();
            adnsfAddress.LoadFromDB(adnsfAddressId);

            return ConvertAddress(adnsfAddress);
        }


        protected net.taxcloud.api.Address ConvertAddress(Address sourceAddress)
        {
            VerifiedAddress verifiedaddress = tc.VerifyAddress(USPSUserID, sourceAddress.Address1, sourceAddress.Address2, sourceAddress.City, sourceAddress.State, sourceAddress.Zip, "");
            if (Localization.ParseNativeInt(verifiedaddress.ErrNumber) == 0)
            {
                return new net.taxcloud.api.Address()
                {
                    Address1 = verifiedaddress.Address1,
                    Address2 = verifiedaddress.Address2,
                    City = verifiedaddress.City,
                    State = verifiedaddress.State,
                    Zip5 = verifiedaddress.Zip5,
                    Zip4 = verifiedaddress.Zip4
                };
            }
            else
            {
                return new net.taxcloud.api.Address()
                {
                    Address1 = sourceAddress.Address1,
                    Address2 = sourceAddress.Address2,
                    City = sourceAddress.City,
                    State = sourceAddress.State,
                    Zip5 = sourceAddress.Zip,
                    Zip4 = ""
                };
            }
        }
        #endregion

        #region Taxcloud CartItems
        private IEnumerable<net.taxcloud.api.CartItem> ConvertCartItems(IEnumerable<CartItem> cartItems, Customer customer, List<CouponObject> CouponList, List<QDObject> QuantityDiscountList)
        {
            IList<net.taxcloud.api.CartItem> refCartItems = new List<net.taxcloud.api.CartItem>();
            foreach (CartItem i in cartItems)
            {
                decimal extendedPrice = Decimal.Zero;

                    if (i.ThisShoppingCart == null)
                    {
                        // Order line items
                        using (var promotionsDataContext = new AspDotNetStorefront.Promotions.Data.EntityContextDataContext())
                        {
                            // Sum the discount for every PromotionLineItem that applies to the current cart item.
                            // A gift product's line item price is already discounted, so don't include the discount when IsAGift is true.
                            var lineItemDiscountAmount = promotionsDataContext.PromotionLineItems
                                .Where(pli => !pli.isAGift)
                                .Where(pli => pli.shoppingCartRecordId == i.ShoppingCartRecordID)
                                .Sum(pli => (decimal?)pli.discountAmount);

                            extendedPrice = i.Price + (lineItemDiscountAmount ?? 0);
                        }
                    }
                    else
                    {
                        // Shopping cart items
                        extendedPrice = Prices.LineItemPrice(i, CouponList, QuantityDiscountList, customer);
                    }
                    net.taxcloud.api.CartItem refCartItem = new net.taxcloud.api.CartItem()
                    {
                        Index = i.ShoppingCartRecordID,
                        ItemID = i.SKU,
                        TIC = Localization.ParseNativeInt(new TaxClass(i.TaxClassID).TaxCode),
                        Price = (double)extendedPrice/i.Quantity,
                        Qty = (float)i.Quantity,
                    };
                    refCartItems.Add(refCartItem);
               
            }
            return refCartItems;

        }


     

        private IEnumerable<net.taxcloud.api.CartItem> ConvertCartItems(CartItemCollection cartItems, Customer customer)
        {
            return ConvertCartItems(cartItems, customer, cartItems.CouponList, cartItems.QuantityDiscountList);
        }

        private IEnumerable<net.taxcloud.api.CartItem> ConvertCartItems(CartItemCollection cartItems, int customerID)
        {
            Customer customer = new Customer(customerID);
            return ConvertCartItems(cartItems, customer);
        }


        #endregion



        private decimal lookupTaxRate(string custID, net.taxcloud.api.CartItem[] refItems, net.taxcloud.api.Address refOrigin, net.taxcloud.api.Address refDest)
        {
            double taxRate = 0.0f;
 
            ExemptionCertificate exemptCert = null;

         
            string _cartID = GetCartID(custID, refDest.Zip5, refItems.First().Index);

            using (SqlConnection conn = DB.dbConn())
            {
                conn.Open();
                using (IDataReader rs = DB.GetRS("select Top 1 certificateID from ShoppingCart where CustomerID=" +  custID, conn))
                {
                    if (rs.Read() && DB.RSField(rs, "certificateID")!="")
                    {
                        exemptCert = new ExemptionCertificate();
                        exemptCert.CertificateID = DB.RSField(rs, "certificateID");
                    }
                }
            }
          

            LookupRsp response = tc.Lookup(ApiLoginID, ApiKey, custID, _cartID, refItems, refOrigin, refDest, DeliveredBySeller, exemptCert);

            if (response.ResponseType == MessageType.Error)
            {
                string errormsg = String.Empty;
                foreach (ResponseMessage message in response.Messages)
                {
                    errormsg += string.Format("TaxCloudError:{0}-{1}", message.ResponseType.ToString(), message.Message);
                }
                throw new Exception(errormsg);
            }
            else
            {
                foreach (CartItemResponse cir in response.CartItemsResponse)
                {
                    taxRate += Math.Round(cir.TaxAmount,2);
                }
            }
            return (decimal)taxRate;
        }

        private IEnumerable<OrderOption> GetOrderOptions(Order order)
        {
            var orderOptions = (order.OrderOptions ?? String.Empty)
                .Split('^')
                .Where(s => !String.IsNullOrEmpty(s))
                .Select(s => s.Split('|'))
                .Select(sa => new OrderOption
                {
                    ID = Convert.ToInt32(sa[0]),
                    Name = sa[2],
                    UniqueID = new Guid(sa[1]),
                    TaxRate = Convert.ToDecimal(CommonLogic.IIF(sa[4].IndexOf("(") == -1, sa[4], sa[4].Substring(0, sa[4].IndexOf("("))).Replace("$", "")),
                    Cost = Convert.ToDecimal(CommonLogic.IIF(sa[3].IndexOf("(") == -1, sa[3], sa[3].Substring(0, sa[3].IndexOf("("))).Replace("$", "")),
                    ImageUrl = sa[5],
                    TaxClassID = sa.Length > 6 ? Convert.ToInt32(sa[6]) : 0,
                });

            return orderOptions;
        }
      
       



		public bool TestAddin(out string reason)
		{
            reason = "";
            return true;
		}

		public decimal GetTaxRate(Customer customer, CartItemCollection cartItems, IEnumerable<OrderOption> orderOptions)
		{
			 
			if(!customer.HasAtLeastOneAddress() || !cartItems.Any())
				return 0m;

			// Return the cached value if it's present and still valid
            string lastCartHash = HttpContext.Current.Items["TaxCloud.CartHash"] as string;
            decimal? lastTaxAmount = HttpContext.Current.Items["TaxCloud.TaxAmount"] as decimal?;
            string currentCartHash = GetCartHash(cartItems, customer, orderOptions);

            if (lastTaxAmount != null && currentCartHash == lastCartHash)
                return lastTaxAmount.Value;

			// Create line items for all cart items and shipping selections
    
            decimal taxAmount = 0M;

            List<CouponObject> CouponList = cartItems.CouponList;
            List<QDObject> QuantityDiscountList = cartItems.QuantityDiscountList;

            if (Shipping.GetDistinctShippingAddressIDs(cartItems).Count ==1)
            {
                
                IEnumerable<net.taxcloud.api.CartItem> refCartitems = ConvertCartItems(cartItems,customer);

                net.taxcloud.api.Address destAddress = ConvertAddress(customer.PrimaryShippingAddress);

                refCartitems = refCartitems.Concat(CreateCartShippingLineItem(customer, cartItems, orderOptions)).Concat(CreateOrderOptionLineItems(orderOptions)) ;
                taxAmount = lookupTaxRate(customer.CustomerID.ToString(), refCartitems.ToArray(), refOrigin, destAddress);
            }
            else
            {
                List<int> shipAddresses = Shipping.GetDistinctShippingAddressIDs(cartItems);
                foreach (int _addressID in shipAddresses)
                {
                    net.taxcloud.api.Address destAddress = ConvertAddress(_addressID);

                    IEnumerable<CartItem> tmpcic = cartItems.Where(r => r.ShippingAddressID == _addressID);

                    IEnumerable<net.taxcloud.api.CartItem> refCartitems = ConvertCartItems(tmpcic, customer, CouponList, QuantityDiscountList);
                    
                    refCartitems = refCartitems.Concat(CreateCartShippingLineItem(customer, tmpcic, orderOptions));
                    if (_addressID == customer.PrimaryShippingAddressID)
                    {
                        refCartitems = refCartitems.Concat(CreateOrderOptionLineItems(orderOptions));
                    }

                    taxAmount += lookupTaxRate(customer.CustomerID.ToString(), refCartitems.ToArray(), refOrigin, destAddress);
                }
            }

			//Cache the tax amount
            HttpContext.Current.Items["TaxCloud.CartHash"] = currentCartHash;
            HttpContext.Current.Items["TaxCloud.TaxAmount"] = taxAmount;

			return taxAmount;
		}

		public void OrderPlaced(Order order)
		{
            //string cartId = GetCartID(order.CustomerID.ToString(), refDest.Zip5, refItems.First().Index);
            string _cartID = string.Empty;
            List<int> shipAddresses = Shipping.GetDistinctShippingAddressIDs(order.CartItems);
            if (shipAddresses.Count==1)
            {
                _cartID = GetCartID(order.CustomerID.ToString(), order.ShippingAddress.m_Zip, order.CartItems.First().ShoppingCartRecordID);
              
                AuthorizedRsp response = tc.Authorized(ApiLoginID, ApiKey, order.CustomerID.ToString(), _cartID, order.OrderNumber.ToString(), DateTime.Now);
                if (response.ResponseType != MessageType.OK)
                {
                    string errormsg = String.Empty;
                    foreach (ResponseMessage message in response.Messages)
                    {
                        errormsg += string.Format("Purchase could not be Authorized({0}-{1})", message.ResponseType.ToString(), message.Message);
                    }
                    
                    throw new Exception(errormsg);
                }
            }
            else
            {
                Customer customer= new Customer(order.CustomerID);
               
                foreach (int _addressID in shipAddresses)
                {
                    net.taxcloud.api.Address destAddress = ConvertAddress(_addressID);

                    IEnumerable<CartItem> tmpcic = order.CartItems.Where(r => r.ShippingAddressID == _addressID);
                    Address _address= new Address();
                    _address.LoadFromDB(_addressID);
                    _cartID = GetCartID(order.CustomerID.ToString(), _address.Zip, tmpcic.First().ShoppingCartRecordID);
                   
                    AuthorizedRsp response = tc.Authorized(ApiLoginID, ApiKey, order.CustomerID.ToString(), _cartID, GetMultipleShippingAddressOrderNumber(order.OrderNumber, _addressID), DateTime.Now);
                    if (response.ResponseType != MessageType.OK)
                    {
                        string errormsg = String.Empty;
                        foreach (ResponseMessage message in response.Messages)
                        {
                            errormsg += string.Format("Purchase could not be Authorized({0}-{1})", message.ResponseType.ToString(), message.Message);
                        }
                        throw new Exception(errormsg);
                    }
                }
            }
		}

		public void CommitTax(Order order)
		{
          
            List<int> shipAddresses = Shipping.GetDistinctShippingAddressIDs(order.CartItems);
            if (shipAddresses.Count == 1)
            {
                CapturedRsp response = tc.Captured(ApiLoginID, ApiKey, order.OrderNumber.ToString());
                if (response.ResponseType != MessageType.OK)
                {
                    string errormsg = String.Empty;
                    foreach (ResponseMessage message in response.Messages)
                    {
                        errormsg += string.Format("Purchase could not be captured({0}-{1})", message.ResponseType.ToString(), message.Message);
                    }
                    throw new Exception(errormsg);
                }
            }
            else
            {

                foreach (int _addressID in shipAddresses)
                {
                   
                    CapturedRsp response = tc.Captured(ApiLoginID, ApiKey, GetMultipleShippingAddressOrderNumber( order.OrderNumber,_addressID));
                    if (response.ResponseType != MessageType.OK)
                    {
                        string errormsg = String.Empty;
                        foreach (ResponseMessage message in response.Messages)
                        {
                            errormsg += string.Format("Purchase could not be captured({0}-{1})", message.ResponseType.ToString(), message.Message);
                        }
                        throw new Exception(errormsg);
                    }
                }
            }
		}

        public void VoidTax(Order order)
        {
            List<int> shipAddresses = Shipping.GetDistinctShippingAddressIDs(order.CartItems);
            if (shipAddresses.Count == 1)
            {
                IEnumerable<net.taxcloud.api.CartItem> refItems = ConvertCartItems(order.CartItems, new Customer(order.CustomerID));
                refItems = refItems.Concat(CreateOrderShippingLineItem(order, shipAddresses.First()));
                ReturnedRsp response = tc.Returned(ApiLoginID, ApiKey, order.OrderNumber.ToString(), refItems.ToArray(), DateTime.Now);
                if (response.ResponseType != MessageType.OK)
                {
                    string errormsg = String.Empty;
                    foreach (ResponseMessage message in response.Messages)
                    {
                        errormsg += string.Format("Purchase could not be Canceled({0}-{1})", message.ResponseType.ToString(), message.Message);
                    }
                    throw new Exception(errormsg);
                }
            }
            else
            {
                Customer customer = new Customer(order.CustomerID);
                List<CouponObject> CouponList= order.CartItems.CouponList;
                List<QDObject> QuantityDiscountList = order.CartItems.QuantityDiscountList;
                IEnumerable<OrderOption> orderOptions=  GetOrderOptions(order);
                foreach (int _addressID in shipAddresses)
                {
                    IEnumerable<CartItem> tmpcic = order.CartItems.Where(r => r.ShippingAddressID == _addressID);

                    IEnumerable<net.taxcloud.api.CartItem> refCartitems = ConvertCartItems(tmpcic, customer,   CouponList, QuantityDiscountList);

                    refCartitems = refCartitems.Concat(CreateOrderShippingLineItem(order, _addressID));
                    if (_addressID == customer.PrimaryShippingAddressID)
                    {
                        refCartitems = refCartitems.Concat(CreateOrderOptionLineItems(orderOptions));
                    }
                    ReturnedRsp response = tc.Returned(ApiLoginID, ApiKey, GetMultipleShippingAddressOrderNumber(order.OrderNumber,_addressID), refCartitems.ToArray(), DateTime.Now);
                    if (response.ResponseType != MessageType.OK)
                    {
                        string errormsg = String.Empty;
                        foreach (ResponseMessage message in response.Messages)
                        {
                            errormsg += string.Format("Purchase could not be Canceled({0}-{1})", message.ResponseType.ToString(), message.Message);
                        }
                        throw new Exception(errormsg);
                    }
                }
            }
        }

		public void IssueRefund(Order order )
		{
            VoidTax(order);
		}

		private void VoidRefunds(Order order)
		{
            VoidTax(order);
		}

		#region Special Item Creation

        private IEnumerable<net.taxcloud.api.CartItem> CreateCartShippingLineItem(int customerID, IEnumerable<CartItem> cartItems, IEnumerable<OrderOption> orderOptions)
        {
            return CreateCartShippingLineItem(new Customer(customerID), cartItems, orderOptions);
        }
		
        private IEnumerable<net.taxcloud.api.CartItem> CreateCartShippingLineItem(Customer customer, IEnumerable<CartItem> cartItems, IEnumerable<OrderOption> orderOptions)
        {
          
            net.taxcloud.api.CartItem lineItem = new net.taxcloud.api.CartItem
            {
                Index = 0,
                ItemID = ShippingItemSku,
                Price = Localization.ParseNativeDouble(Prices.ShippingTotal(true, true, new CartItemCollection(cartItems), customer, orderOptions).ToString()),
                Qty =  1.0f,
                TIC = APiShippingTaxClassCode,
            };
            yield return lineItem;
        }

        private IEnumerable<net.taxcloud.api.CartItem> CreateOrderShippingLineItem(Order order, int destAddressId)
        {
           
            var shippingAmount = new OrderShipmentCollection(order.OrderNumber)
                .Where(os => os.AddressID == destAddressId)
                .Select(os => os.ShippingTotal)
                .FirstOrDefault();

            net.taxcloud.api.CartItem lineItem = new net.taxcloud.api.CartItem
            {
                Index = 0,
                ItemID = ShippingItemSku,
                Price =(double)shippingAmount,
                Qty = 1.0f,
                TIC = APiShippingTaxClassCode,
            };
            yield return lineItem;
 
        }

 
        private IEnumerable<net.taxcloud.api.CartItem> CreateOrderOptionLineItems(IEnumerable<OrderOption> orderOptions)
		{
			return orderOptions.Select(oo => new
				{
					OrderOption = oo,
					TaxClass = new TaxClass(oo.TaxClassID),
				})
                .Select(o => new net.taxcloud.api.CartItem
				{ 
                    Index=1,
                    ItemID = OrderOptionItemSku + o.OrderOption.Name.Replace(" ", "_"),
                    Price= (double)o.OrderOption.Cost,
                    Qty= 1f,
                    TIC= Localization.ParseNativeInt(o.TaxClass.TaxCode),
				}
			);
		}

      
		#endregion

        private string GetMultipleShippingAddressOrderNumber(int ordernumber, int addressID)
        {
            return string.Format("{0}-{1}",ordernumber.ToString(), addressID.ToString());
        }

        private string GetCartID(string customerID, string descZip, int firstShoppingCartRecordID)
        {
            return string.Format("{0}-{1}-{2}", customerID, descZip, firstShoppingCartRecordID);
        }

        private string GetCartHash(CartItemCollection cartItems, Customer customer, IEnumerable<OrderOption> orderOptions)
        {
           

            string cartComposite = cartItems
                .Select(c => new
                {
                    CartItem = c,
                    Address = new Address(c.ShippingAddressID)
                })
                .Aggregate(
                    String.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}_{9}_{10}",
                        refOrigin.City,
                        refOrigin.State,
                        refOrigin.Zip5,
                        customer.PrimaryShippingAddress.City,
                        customer.PrimaryShippingAddress.State,
                        customer.PrimaryShippingAddress.Zip,
                        customer.LevelHasNoTax,
                        cartItems.DiscountResults.Sum(dr => dr.OrderTotal),
                        cartItems.DiscountResults.Sum(dr => dr.ShippingTotal),
                        cartItems.DiscountResults.Sum(dr => dr.LineItemTotal),
                        orderOptions.Aggregate("_", (s, oo) => String.Format("{0}_{1}_{2}_{3}", s, oo.ID, oo.TaxClassID, oo.Cost))),
                    (s, o) => String.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}",
                        s,
                        o.CartItem.ShoppingCartRecordID,
                        o.CartItem.VariantID,
                        o.CartItem.Quantity,
                        o.CartItem.Price,
                        o.CartItem.IsTaxable,
                        o.CartItem.TaxClassID,
                        o.CartItem.ShippingAddressID,
                        o.CartItem.ShippingMethodID)
                );

            using (var md5 = System.Security.Cryptography.MD5CryptoServiceProvider.Create())
            {
                var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(cartComposite));
                return hash.Aggregate(String.Empty, (s, b) => String.Format("{0}{1:x2}", s, b));
            }
        }
    }
}
