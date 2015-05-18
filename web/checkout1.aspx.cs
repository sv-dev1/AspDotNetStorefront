// --------------------------------------------------------------------------------
// Copyright AspDotNetStorefront.com. All Rights Reserved.
// http://www.aspdotnetstorefront.com
// For details on this license please visit the product homepage at the URL above.
// THE ABOVE NOTICE MUST REMAIN INTACT. 
// --------------------------------------------------------------------------------
using System;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using AspDotNetStorefrontCore;
using AspDotNetStorefrontGateways;
using System.Data.SqlClient;
using System.Collections.Generic;
using AspDotNetStorefrontControls;
using AspDotNetStorefrontCore.ShippingCalculation;
using AspDotNetStorefrontGateways.Processors;

namespace AspDotNetStorefront
{
    /// <summary>
    /// Summary description for checkout1.
    /// </summary>
    public partial class checkout1 : SkinBase
    {
        private bool SkipRegistration = false;
        private ShoppingCart cart;
        private bool RequireSecurityCode = false;
        private bool AllowShipToDifferentThanBillTo = false;
        private String ReturnURL = String.Empty;
        private Address BillingAddress = new Address(); // qualification needed for vb.net (not sure why)
        private Address ShippingAddress = new Address(); // qualification needed for vb.net (not sure why)
        private Shipping.ShippingCalculationEnum ShipCalcType;
        private bool ShippingRequiresAddressInfo = true;
        
        private string AllowedPaymentMethods = String.Empty;
        private string GW = String.Empty;
        private GatewayProcessor GWActual;
        private string SelectedPaymentType = String.Empty;
        private bool useLiveTransactions = false;
        private decimal CartTotal = Decimal.Zero;
        private decimal NetTotal = Decimal.Zero;
        bool AnyShippingMethodsFound = false;
        private Boolean DisablePasswordAutocomplete
        {
            get { return AppLogic.AppConfigBool("DisablePasswordAutocomplete"); }
        }

        #region "Properties"
        
        /// <summary>
        /// Whether or no terms checkbox has been check and termsAndConditions are required
        /// </summary>
        private bool TermsAndConditionsAccepted
        {
            get
            {return !RequireTerms || (chkTermsAccepted.Checked && !AppLogic.ProductIsMLExpress());}
        }
        /// <summary>
        /// AppConfig : RequireTermsAndConditionsAtCheckout
        /// </summary>
        private bool RequireTerms
        {
            get
            {return AppLogic.AppConfigBool("RequireTermsAndConditionsAtCheckout");}
        }

        #endregion

        protected override void OnInit(EventArgs e)
        {
            cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            if (cart.HasCoupon())
            {
                string CouponName = cart.Coupon.CouponCode;
                cart.ClearCoupon();
                cart.SetCoupon(CouponName, true);
                //cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            }  
                    
            PopulateShippingMethods();
            PopulateAddressControlValues(ctrlBillingAddress, AddressTypes.Billing);
            PopulateAddressControlValues(ctrlShippingAddress, AddressTypes.Shipping);
            base.OnInit(e);
        }

        private void PopulateShippingMethods()
        {
            ShippingMethodCollection shippingMethods = cart.GetShippingMethods(ThisCustomer.PrimaryShippingAddress);
            if (shippingMethods.Count > 0)
            {
                AnyShippingMethodsFound = true;
            }

            InitializeShippingMethodDisplayFormat(shippingMethods);
            if (!AppLogic.AppConfigBool("FreeShippingAllowsRateSelection") && (cart.IsAllFreeShippingComponents() || (!AnyShippingMethodsFound && cart.ShippingIsFree) || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CustomerLevelHasFreeShipping || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.ExceedsFreeShippingThreshold || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CouponHasFreeShipping))
            {
                ctrlShippingMethods.DataSource = null;
            }
            else
            {
                ctrlShippingMethods.DataSource = shippingMethods;
            }

            if (ThisCustomer.PrimaryShippingAddressID > 0)
            {
                ctrlShippingMethods.Visible = true;
            }
            else
            {
                ctrlShippingMethods.Visible = false;
            }


            ctrlShoppingCart.DataSource = cart.CartItems;
            ctrlShoppingCart.DataBind();

            ctrlCartSummary.DataSource = cart;

            BillingAddress.LoadByCustomer(ThisCustomer.CustomerID, ThisCustomer.PrimaryBillingAddressID, AddressTypes.Billing);
            ShippingAddress.LoadByCustomer(ThisCustomer.CustomerID, ThisCustomer.PrimaryShippingAddressID, AddressTypes.Shipping);
        }
            
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.CacheControl = "private";
            Response.Expires = -1;
            Response.AddHeader("pragma", "no-cache");

            RequireSecurePage();
            ThisCustomer.RequireCustomerRecord();

            //When the user wants to skip from registering, we must also consider if AnonCheckoutReqEmail is set to true 
            //to prevent user from checking out without a valid email address. 
            SkipRegistration = (AppLogic.AppConfigBool("PasswordIsOptionalDuringCheckout") && AppLogic.AppConfigBool("HidePasswordFieldDuringCheckout"));
            RequireSecurityCode = AppLogic.AppConfigBool("SecurityCodeRequiredOnCheckout1DuringCheckout");
            SectionTitle = AppLogic.GetString("createaccount.aspx.2", SkinID, ThisCustomer.LocaleSetting);

            // -----------------------------------------------------------------------------------------------
            // NOTE ON PAGE LOAD LOGIC:
            // We are checking here for required elements to allowing the customer to stay on this page.
            // Many of these checks may be redundant, and they DO add a bit of overhead in terms of db calls, but ANYTHING really
            // could have changed since the customer was on the last page. Remember, the web is completely stateless. Assume this
            // page was executed by ANYONE at ANYTIME (even someone trying to break the cart). 
            // It could have been yesterday, or 1 second ago, and other customers could have purchased limitied inventory products, 
            // coupons may no longer be valid, etc, etc, etc...
            // -----------------------------------------------------------------------------------------------

            if (DisablePasswordAutocomplete)
            {
                AppLogic.DisableAutocomplete(password);
                AppLogic.DisableAutocomplete(password2);
            }

            if (cart.IsEmpty())
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1");
            }

            ErrorMessage err;

            if (cart.InventoryTrimmed)
            {
                err = new ErrorMessage(Server.HtmlEncode(AppLogic.GetString("shoppingcart.aspx.3", SkinID, ThisCustomer.LocaleSetting)));
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&errormsg=" + err.MessageId);
            }

            if (cart.RecurringScheduleConflict)
            {
                err = new ErrorMessage(Server.HtmlEncode(AppLogic.GetString("shoppingcart.aspx.19", SkinID, ThisCustomer.LocaleSetting)));
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&errormsg=" + err.MessageId);
            }

            if (cart.HasCoupon() && !cart.CouponIsValid)
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&discountvalid=false");
            }

            if (!cart.MeetsMinimumOrderAmount(AppLogic.AppConfigUSDecimal("CartMinOrderAmount")))
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1");
            }

            if (!cart.MeetsMinimumOrderQuantity(AppLogic.AppConfigUSInt("MinCartItemsBeforeCheckout")))
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1");
            }

			AspDotNetStorefrontCore.CheckOutPageController checkoutController = new CheckOutPageController(ThisCustomer, cart);
			if (!checkoutController.CanUseOnePageCheckout())
			{
				Response.Redirect(checkoutController.GetContinueCheckoutPage());
			}

            AllowShipToDifferentThanBillTo = AppLogic.AppConfigBool("AllowShipToDifferentThanBillTo");
            if (cart.IsAllDownloadComponents() ||
                cart.IsAllSystemComponents())
            {
                AllowShipToDifferentThanBillTo = false;
            }

            ReturnURL = CommonLogic.QueryStringCanBeDangerousContent("ReturnURL");
            if (ReturnURL.IndexOf("<script>", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                throw new ArgumentException("SECURITY EXCEPTION");
            }
            ErrorMsgLabel.Text = "";

            if (!AppLogic.AppConfigBool("RequireOver13Checked"))
            {
                pnlOver13.Visible = false;
                Literal2.Visible = false;
                SkipRegOver13.Visible = false;
            }

            ShipCalcType = Shipping.GetActiveShippingCalculationID();
            if (ShipCalcType == Shipping.ShippingCalculationEnum.AllOrdersHaveFreeShipping || ShipCalcType == Shipping.ShippingCalculationEnum.CalculateShippingByTotal || ShipCalcType == Shipping.ShippingCalculationEnum.CalculateShippingByWeight || ShipCalcType == Shipping.ShippingCalculationEnum.UseFixedPercentageOfTotal || ShipCalcType == Shipping.ShippingCalculationEnum.UseFixedPrice ||
                ShipCalcType == Shipping.ShippingCalculationEnum.UseIndividualItemShippingCosts)
            {
                // these types of shipping calcs do NOT require address info, so show them right now on the page:
             
                ShippingRequiresAddressInfo = false;
            }

            if (!ShippingRequiresAddressInfo)
            {
               
            }

            GW = AppLogic.ActivePaymentGatewayCleaned();
            GWActual = GatewayLoader.GetProcessor(GW);
            useLiveTransactions = AppLogic.AppConfigBool("UseLiveTransactions");
            CartTotal = cart.Total(true);
            NetTotal = CartTotal - CommonLogic.IIF(cart.Coupon.CouponType == CouponTypeEnum.GiftCard, CommonLogic.IIF(CartTotal < cart.Coupon.DiscountAmount, CartTotal, cart.Coupon.DiscountAmount), 0);

            if (!IsPostBack)
            {
                ViewState["SelectedPaymentType"] = string.Empty;
                InitializeValidationErrorMessages();
                InitializePageContent();                
            }
            GetJavaScriptFunctions();
        }
        
        private void InitializeShippingMethodDisplayFormat(ShippingMethodCollection shippingMethods)
        {
            foreach (ShippingMethod shipMethod in shippingMethods)
            {
                string freightDisplayText = string.Empty;

                if (!string.IsNullOrEmpty(ThisCustomer.CurrencySetting))
                {
                    freightDisplayText = Localization.CurrencyStringForDisplayWithExchangeRate(shipMethod.Freight, ThisCustomer.CurrencySetting);
                    if (shipMethod.ShippingIsFree && Shipping.ShippingMethodIsInFreeList(shipMethod.Id))
                    {
                        freightDisplayText = AppLogic.GetString("shoppingcart.aspx.16", SkinID, ThisCustomer.LocaleSetting);
                    }
                }
                shipMethod.DisplayFormat = string.Format("{0} ({1})", shipMethod.Name, freightDisplayText);
            }
        }


        #region EventHandlers

        public void valCustSecurityCode_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = (SecurityCode.Text.Trim() == Session["SecurityCode"].ToString());
        }

        public void ValidatePassword(object source, ServerValidateEventArgs args)
        {
            string pwd1 = ViewState["custpwd"].ToString();
            string pwd2 = ViewState["custpwd2"].ToString();

            if (ThisCustomer.IsRegistered)
            {
                args.IsValid = true;
            }
            else if (pwd1.Length == 0)
            {
                args.IsValid = false;
                valPassword.ErrorMessage = AppLogic.GetString("createaccount.aspx.20", SkinID, ThisCustomer.LocaleSetting);
            }
            else if (pwd1.Trim().Length == 0)
            {
                args.IsValid = false;
                valPassword.ErrorMessage = AppLogic.GetString("account.aspx.74", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
            }
            else if (pwd1 == pwd2)
            {
                try
                {
                    valPassword.ErrorMessage = AppLogic.GetString("account.aspx.7", SkinID, ThisCustomer.LocaleSetting);
                    if (AppLogic.AppConfigBool("UseStrongPwd") || ThisCustomer.IsAdminUser)
                    {
                        if (Regex.IsMatch(pwd1, AppLogic.AppConfig("CustomerPwdValidator"), RegexOptions.Compiled))
                        {
                            args.IsValid = true;
                        }
                        else
                        {
                            args.IsValid = false;
                            valPassword.ErrorMessage = AppLogic.GetString("account.aspx.69", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                        }
                    }
                    else
                    {
                        args.IsValid = (pwd1.Length > 4);
                    }
                }
                catch
                {
                    AppLogic.SendMail("Invalid Password Validation Pattern", "", false, AppLogic.AppConfig("MailMe_ToAddress"), AppLogic.AppConfig("MailMe_ToAddress"), AppLogic.AppConfig("MailMe_ToAddress"), AppLogic.AppConfig("MailMe_ToAddress"), "", "", AppLogic.MailServer());
                    throw new Exception("Password validation expression is invalid, please notify site administrator");
                }
            }
            else
            {
                args.IsValid = false;
                valPassword.ErrorMessage = AppLogic.GetString("createaccount.aspx.80", SkinID, ThisCustomer.LocaleSetting);
            }

            if (!args.IsValid)
            {
                ViewState["custpwd"] = "";
                ViewState["custpwd2"] = "";
            }
        }

        public void btnRecalcShipping_OnClick(object source, EventArgs e)
        {
            String EMailField = CommonLogic.IIF(SkipRegistration, txtSkipRegEmail.Text.ToLower().Trim(), EMail.Text.ToLower().Trim());
 
            bool acctvalid = false;
			if(!SkipRegistration)
			{
				SetPasswordFields();
				acctvalid = AccountIsValid();

				String PWD = ViewState["custpwd"].ToString();
				Password p = new Password(PWD);
				String newpwd = p.SaltedPassword;
				Nullable<int> newsaltkey = p.Salt;
				if(ThisCustomer.Password.Length != 0)
				{
					// do NOT allow passwords to be changed on this page. this is only for creating an account.
					// if they want to change their password, they must use their account page
					newpwd = null;
					newsaltkey = null;
				}




				if(acctvalid)
				{
					ThisCustomer.UpdateCustomer(
						/*CustomerLevelID*/ null,
						/*EMail*/ EMailField,
						/*SaltedAndHashedPassword*/ newpwd,
						/*SaltKey*/ newsaltkey,
						/*DateOfBirth*/ null,
						/*Gender*/ null,
						/*FirstName*/ FirstName.Text.Trim(),
						/*LastName*/ LastName.Text.Trim(),
						/*Notes*/ null,
						/*SkinID*/ null,
						/*Phone*/ Phone.Text.Trim(),
						/*AffiliateID*/ null,
						/*Referrer*/ null,
						/*CouponCode*/ null,
						/*OkToEmail*/ CommonLogic.IIF(OKToEMailYes.Checked, 1, 0),
						/*IsAdmin*/ null,
						/*BillingEqualsShipping*/ CommonLogic.IIF(ShippingEqualsBilling.Checked, 1, 0),
						/*LastIPAddress*/ null,
						/*OrderNotes*/ null,
						/*SubscriptionExpiresOn*/ null,
						/*RTShipRequest*/ null,
						/*RTShipResponse*/ null,
						/*OrderOptions*/ null,
						/*LocaleSetting*/ null,
						/*MicroPayBalance*/ null,
						/*RecurringShippingMethodID*/ null,
						/*RecurringShippingMethod*/ null,
						/*BillingAddressID*/ null,
						/*ShippingAddressID*/ null,
						/*GiftRegistryGUID*/ null,
						/*GiftRegistryIsAnonymous*/ null,
						/*GiftRegistryAllowSearchByOthers*/ null,
						/*GiftRegistryNickName*/ null,
						/*GiftRegistryHideShippingAddresses*/ null,
						/*CODCompanyCheckAllowed*/ null,
						/*CODNet30Allowed*/ null,
						/*ExtensionData*/ null,
						/*FinalizationData*/ null,
						/*Deleted*/ null,
						/*Over13Checked*/ CommonLogic.IIF(Over13.Checked || SkipRegOver13.Checked, 1, 0),
						/*CurrencySetting*/ null,
						/*VATSetting*/ null,
						/*VATRegistrationID*/ null,
						/*StoreCCInDB*/ null,
						/*IsRegistered*/ CommonLogic.IIF(SkipRegistration, 0, 1),
						/*LockedUntil*/ null,
						/*AdminCanViewCC*/ null,
						/*BadLogin*/ null,
						/*Active*/ null,
						/*PwdChangeRequired*/ null,
						/*RegisterDate*/ null,
						/*StoreId*/AppLogic.StoreID()
						);
					pnlErrorMsg.Visible = false;
				}
				else
				{
					ErrorMsgLabel.Text += "<br /><br /> " + AppLogic.GetString("checkout1.aspx.9", ThisCustomer.SkinID, ThisCustomer.LocaleSetting) + "<br /><br />";

					foreach(IValidator aValidator in Validators)
					{
						if(!aValidator.IsValid)
						{
							ErrorMsgLabel.Text += "&bull; " + aValidator.ErrorMessage + "<br />";
						}
					}
					ErrorMsgLabel.Text += "<br />";

					pnlErrorMsg.Visible = true;
					return;
				}
			}

            bool shippingvalid = ShippingIsValid();
            if (AllowShipToDifferentThanBillTo)
            {
                if (shippingvalid)
                {
                    ShippingAddress = new Address();
                    ShippingAddress.LastName = ctrlShippingAddress.LastName.Trim();
                    ShippingAddress.FirstName = ctrlShippingAddress.FirstName;
                    ShippingAddress.Phone = ctrlShippingAddress.PhoneNumber;
                    ShippingAddress.Company = ctrlShippingAddress.Company;
                    ShippingAddress.ResidenceType = (ResidenceTypes)Enum.Parse(typeof(ResidenceTypes), ctrlShippingAddress.ResidenceType, true);//(ResidenceTypes)ctrlShippingAddress.AddressType;
                    ShippingAddress.Address1 = ctrlShippingAddress.Address1;
                    ShippingAddress.Address2 = ctrlShippingAddress.Address2;
                    ShippingAddress.Suite = ctrlShippingAddress.Suite;
                    ShippingAddress.City = ctrlShippingAddress.City;
                    ShippingAddress.State = ctrlShippingAddress.State;
                    ShippingAddress.Zip = ctrlShippingAddress.ZipCode;
                    ShippingAddress.Country = ctrlShippingAddress.Country;
                    ShippingAddress.EMail = EMailField;

                    ShippingAddress.InsertDB(ThisCustomer.CustomerID);
                    ShippingAddress.MakeCustomersPrimaryAddress(AddressTypes.Shipping);
                    ThisCustomer.PrimaryShippingAddressID = ShippingAddress.AddressID;
                }
                else
                {
                    ShippingEqualsBilling.Checked = false;

                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Visible = true;

                    if (ErrorMsgLabel.Text.Length == 0)
                    {
                        ErrorMsgLabel.Text += "<br /><br /> " + String.Format(AppLogic.GetString("checkout1.aspx.7", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), AppLogic.GetString("order.cs.55", ThisCustomer.SkinID, ThisCustomer.LocaleSetting)) + "<br /><br />";
                    }


                    foreach (IValidator aValidator in Validators)
                    {
                        if (!aValidator.IsValid &&
                            ErrorMsgLabel.Text.IndexOf(aValidator.ErrorMessage) == -1)
                        {
                            ErrorMsgLabel.Text += "&bull; " + aValidator.ErrorMessage + "<br />";
                        }
                    }
                    ErrorMsgLabel.Text += "<br />";
                }
            }


            bool billingvalid = BillingIsValid();
            if (billingvalid)
            {
                BillingAddress = new Address();

                BillingAddress.LastName = ctrlBillingAddress.LastName;
                BillingAddress.FirstName = ctrlBillingAddress.FirstName;
                BillingAddress.Phone = ctrlBillingAddress.PhoneNumber;
                BillingAddress.Company = ctrlBillingAddress.Company;
                BillingAddress.ResidenceType = (ResidenceTypes)Enum.Parse(typeof(ResidenceTypes), ctrlBillingAddress.ResidenceType, true);//(ResidenceTypes)ctrlBillingAddress.AddressType;
                BillingAddress.Address1 = ctrlBillingAddress.Address1;
                BillingAddress.Address2 = ctrlBillingAddress.Address2;
                BillingAddress.Suite = ctrlBillingAddress.Suite; 
                BillingAddress.City = ctrlBillingAddress.City;
                BillingAddress.State = ctrlBillingAddress.State;
                BillingAddress.Zip = ctrlBillingAddress.ZipCode; 
                BillingAddress.Country = ctrlBillingAddress.Country;
                BillingAddress.EMail = EMailField;

                BillingAddress.InsertDB(ThisCustomer.CustomerID);
                BillingAddress.MakeCustomersPrimaryAddress(AddressTypes.Billing);
                ThisCustomer.PrimaryBillingAddressID = BillingAddress.AddressID;
                if (!AllowShipToDifferentThanBillTo)
                {
                    ThisCustomer.PrimaryShippingAddressID = BillingAddress.AddressID;
                    BillingAddress.MakeCustomersPrimaryAddress(AddressTypes.Shipping);
                }
            }
            else
            {
                if (ErrorMsgLabel.Text.Length == 0)
                {
                    ErrorMsgLabel.Text += "<br /><br /> " + String.Format(AppLogic.GetString("checkout1.aspx.7", ThisCustomer.SkinID, ThisCustomer.LocaleSetting), AppLogic.GetString("order.cs.55", ThisCustomer.SkinID, ThisCustomer.LocaleSetting)) + "<br /><br />";
                }

                foreach (IValidator aValidator in Validators)
                {
                    if (!aValidator.IsValid &&
                        ErrorMsgLabel.Text.IndexOf(aValidator.ErrorMessage) == -1)
                    {
                        ErrorMsgLabel.Text += "&bull; " + aValidator.ErrorMessage + "<br />";
                    }
                }
                ErrorMsgLabel.Text += "<br />";
            }

			if(billingvalid && (!AllowShipToDifferentThanBillTo || shippingvalid))
			{
				ctrlBillingAddress.AllowEdit = false;
				ctrlShippingAddress.AllowEdit = false;

				if(SkipRegistration)
				{
					lnkEditAnonymousBillingInfo.Visible = true;
					lnkEditAnonymousShippingInfo.Visible = true;
				}

				cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
				InitializePageContent();
			}
        }

        public void ValidateAccountEmail(object source, ServerValidateEventArgs args)
        {
            //filter the email address being inputted by the user whether it came from an anon customer or registered one  
            if (SkipRegistration && !ThisCustomer.IsRegistered)
            {
                bool NewEMailPassedDuplicationRules = Customer.NewEmailPassesDuplicationRules(txtSkipRegEmail.Text, ThisCustomer.CustomerID, SkipRegistration);
                args.IsValid = (txtSkipRegEmail.Text.Trim().Length > 0) && Regex.IsMatch(txtSkipRegEmail.Text, @"^[a-zA-Z0-9][-\w\.]*@([a-zA-Z0-9][\w\-]*\.)+[a-zA-Z]{2,3}$", RegexOptions.Compiled) && NewEMailPassedDuplicationRules;
                if (txtSkipRegEmail.Text.Trim().Length == 0)
                {
                    valReqSkipRegEmail.ErrorMessage = AppLogic.GetString("createaccount.aspx.81", SkinID, ThisCustomer.LocaleSetting);
                }
                else if (!NewEMailPassedDuplicationRules)
                {
                    valReqSkipRegEmail.ErrorMessage = AppLogic.GetString("createaccount_process.aspx.1", SkinID, ThisCustomer.LocaleSetting);
                }
                else if (Regex.IsMatch(txtSkipRegEmail.Text, @"^[a-zA-Z0-9][-\w\.]*@([a-zA-Z0-9][\w\-]*\.)+[a-zA-Z]{2,3}$", RegexOptions.Compiled))
                {
                    valReqSkipRegEmail.ErrorMessage = AppLogic.GetString("createaccount_process.aspx.17", SkinID, ThisCustomer.LocaleSetting);
                }
            }
            else
            {
                bool NewEMailPassedDuplicationRules = Customer.NewEmailPassesDuplicationRules(EMail.Text, ThisCustomer.CustomerID, SkipRegistration);
                args.IsValid = (EMail.Text.Trim().Length > 0) && Regex.IsMatch(EMail.Text, @"^[a-zA-Z0-9][-\w\.]*@([a-zA-Z0-9][\w\-]*\.)+[a-zA-Z]{2,3}$", RegexOptions.Compiled) && NewEMailPassedDuplicationRules;
                if (EMail.Text.Trim().Length == 0)
                {
                    valAcctEmail.ErrorMessage = AppLogic.GetString("createaccount.aspx.16", SkinID, ThisCustomer.LocaleSetting);
                }
                else if (!Regex.IsMatch(EMail.Text, @"^[a-zA-Z0-9][-\w\.]*@([a-zA-Z0-9][\w\-]*\.)+[a-zA-Z]{2,3}$", RegexOptions.Compiled))
                {
                    valAcctEmail.ErrorMessage = AppLogic.GetString("createaccount.aspx.17", SkinID, ThisCustomer.LocaleSetting);
                }
                else if (!NewEMailPassedDuplicationRules)
                {
                    valAcctEmail.ErrorMessage = AppLogic.GetString("createaccount_process.aspx.1", SkinID, ThisCustomer.LocaleSetting);
                }
            }
        }

		protected void EditAnonymousBillingInfo_Click(object sender, EventArgs e)
		{
			EnableAnonymousAddressesEditing();
		}

		protected void EditAnonymousShippingInfo_Click(object sender, EventArgs e)
		{
			EnableAnonymousAddressesEditing();
		}
		
		protected void EnableAnonymousAddressesEditing()
		{
			lnkEditAnonymousShippingInfo.Visible = false;
			ctrlShippingAddress.AllowEdit = true;

			lnkEditAnonymousBillingInfo.Visible = false;
			ctrlBillingAddress.AllowEdit = true;

			divShippingOptions.Visible = false;
			divRecalcShipping.Visible = true;
		}

        #endregion

        #region Private Functions

        private void InitializePageContentPayment()
        {
            ctrlShoppingCart.HeaderTabImageURL = AppLogic.SkinImage("OrderInfo.gif");

            if (cart.HasCoupon())
            {
                string CouponName = cart.Coupon.CouponCode;
                cart.ClearCoupon();
                cart.SetCoupon(CouponName, true);
                cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            }

            billinginfo_gif.ImageUrl = AppLogic.SkinImage("billinginfo.gif");
            shippinginfo_gif.ImageUrl = AppLogic.SkinImage("shippinginfo.gif");

            shippingselect_gif.ImageUrl = AppLogic.SkinImage("shippingselect.gif");
            paymentselect_gif.ImageUrl = AppLogic.SkinImage("paymentselect.gif");

            InitializeAccountInfo();
            InitializePaymentOptions(ref cart);

            OrderSummary.Text = cart.DisplaySummary(true, true, true, true, false);
        }

        private void InitializePageContent()
        {
            ctrlShoppingCart.HeaderTabImageURL = AppLogic.SkinImage("OrderInfo.gif");

            if (cart.HasCoupon())
            {
                string CouponName = cart.Coupon.CouponCode;
                cart.ClearCoupon();
                cart.SetCoupon(CouponName, true);
                //cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            }

            billinginfo_gif.ImageUrl = AppLogic.SkinImage("billinginfo.gif");
            shippinginfo_gif.ImageUrl = AppLogic.SkinImage("shippinginfo.gif");

            shippingselect_gif.ImageUrl = AppLogic.SkinImage("shippingselect.gif");
            paymentselect_gif.ImageUrl = AppLogic.SkinImage("paymentselect.gif");

            InitializeAccountInfo();
            InitializeShippingOptions(ref cart);
            InitializePaymentOptions(ref cart);

            OrderSummary.Text = cart.DisplaySummary(true, true, true, true, false);

			if(!AllowShipToDifferentThanBillTo)
			{
				billinginfo_gif.ImageUrl = AppLogic.LocateImageURL("~/App_Themes/Skin_" + SkinID.ToString() + "/images/shippingandbillinginfo.gif");
				tdShipingInfo.Visible = false;
				tdBillingInfo.ColSpan = 2;
				checkout1aspx10.Text = AppLogic.GetString("checkout1.aspx.12", SkinID, ThisCustomer.LocaleSetting);
				checkout1aspx10Anonymous.Text = AppLogic.GetString("checkout1.aspx.12", SkinID, ThisCustomer.LocaleSetting);
			}

			if(SkipRegistration)
			{
				lnkEditBillingInfo.Visible = false;
				lnkEditShippingInfo.Visible = false;

				if(ThisCustomer.PrimaryBillingAddressID != 0)
				{
					lnkEditAnonymousBillingInfo.Visible = true;
					ctrlBillingAddress.AllowEdit = false;

					lnkEditAnonymousShippingInfo.Visible = true;
					ctrlShippingAddress.AllowEdit = false;
				}
			}
        }

        private void InitializeValidationErrorMessages()
        {
            valReqFirstName.ErrorMessage = AppLogic.GetString("createaccount.aspx.82", SkinID, ThisCustomer.LocaleSetting);
            valReqLastName.ErrorMessage = AppLogic.GetString("createaccount.aspx.83", SkinID, ThisCustomer.LocaleSetting);
            valPassword.ErrorMessage = AppLogic.GetString("createaccount.aspx.20", SkinID, ThisCustomer.LocaleSetting);
            valReqPhone.ErrorMessage = AppLogic.GetString("createaccount.aspx.24", SkinID, ThisCustomer.LocaleSetting);
            valReqSecurityCode.ErrorMessage = AppLogic.GetString("signin.aspx.20", SkinID, ThisCustomer.LocaleSetting);     
            valRegExSkipRegEmail.ErrorMessage = AppLogic.GetString("createaccount.aspx.17", SkinID, ThisCustomer.LocaleSetting);
        }

        private void GetJavaScriptFunctions()
        {

            StringBuilder s = new StringBuilder("<script type=\"text/javascript\" Language=\"JavaScript\">\n");
            s.Append("function EscapeHtml(eventTarget, eventArgument){\n");
            s.Append("\t var retVal = validateCheckout();\n");
            s.Append("\t alert(eventTarget);\n");
            s.Append("\t if(!retVal) return false;\n");
            s.Append("\t return netPostBack (eventTarget, eventArgument);\n");
            s.Append("}\n\n");
            s.Append("function validateCheckout(){\n");
            s.Append("\t var btn = document.getElementById('" + btnCheckOut.ClientID + "');\n");
            s.Append("\t var retVal = validateForm(document.getElementById('" + Page.Form.ClientID + "'));\n");
            s.Append("\t if(!retVal) {btn.disabled = false; return false;}\n");
            s.Append("\t retVal = ValidateAccountInfo();\n");
            s.Append("\t if(!retVal) {btn.disabled = false; return false;}\n");
            //s.Append("\t retVal = ValidateShippingInfo();\n");
            //s.Append("\t if(!retVal) {btn.disabled = false; return false;}\n");
            s.Append("\t retVal = ValidatePaymentInfo();\n");
            s.Append("\t if(!retVal) {btn.disabled = false; return false;}\n");
            s.Append("\t retVal = ValidateTerms();\n");
            s.Append("\t if(!retVal) {btn.disabled = false; return false;}\n");
            s.Append("\t btn.disabled = false;\n");
            s.Append("\t return true;\n");
            s.Append("}\n\n");

            s.Append("function ValidateTerms(){\n");
            s.Append("return true;\n");
            s.Append("}\n\n");

            s.Append("function ValidateShippingInfo(){\n");

            if (AllowShipToDifferentThanBillTo && !AppLogic.AppConfigBool("SkipShippingOnCheckout") && !cart.IsAllDownloadComponents() &&
               !cart.IsAllSystemComponents() && !cart.NoShippingRequiredComponents() && !cart.IsAllEmailGiftCards())
            {
                if (cart.CartAllowsShippingMethodSelection &&
                    (!AppLogic.AppConfigBool("FreeShippingAllowsRateSelection") && (cart.IsAllFreeShippingComponents() || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CustomerLevelHasFreeShipping || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.ExceedsFreeShippingThreshold || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CouponHasFreeShipping)))
                {
                    s.Append("return true;\n");
                }
                else if (!AppLogic.AppConfigBool("FreeShippingAllowsRateSelection") && (cart.IsAllFreeShippingComponents() || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CouponHasFreeShipping || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CustomerLevelHasFreeShipping))
                {
                    s.Append("return true;\n");
                }
                else
                {
                    s.Append("\tvar retVal = CheckoutShippingForm_Validator();");
                    s.Append("return retVal;\n");
                }
            }
            else
            {
                s.Append("return true;\n");
            }
            s.Append("}\n\n");

            s.Append("function ValidatePaymentInfo(){\n");
            s.Append("return true;\n");
            s.Append("}\n\n");

            s.Append("function ValidateAccountInfo(){\n");
            s.Append("return true;");
            s.Append("}\n\n");
          
            Dictionary<string, string> billingClientIDs = GetAddressChildsClientID(this.ctrlBillingAddress);
            Dictionary<string, string> shippingClientIDs = GetAddressChildsClientID(this.ctrlShippingAddress);
            
           
            ShippingEqualsBilling.Attributes.Add("onclick", "copybilling(this.form);");
            s.Append("function copybilling(theForm){ ");

            s.Append("if (theForm." + ShippingEqualsBilling.ClientID + ".checked){ ");
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["FirstName"], billingClientIDs["FirstName"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["LastName"], billingClientIDs["LastName"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["Phone"], billingClientIDs["Phone"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["Company"], billingClientIDs["Company"]);
            s.AppendFormat("	theForm.{0}.selectedIndex = theForm.{1}.selectedIndex;", shippingClientIDs["AddressType"], billingClientIDs["AddressType"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["Address1"], billingClientIDs["Address1"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["Address2"], billingClientIDs["Address2"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["Suite"], billingClientIDs["Suite"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["City"], billingClientIDs["City"]);
            s.AppendFormat("	theForm.{0}.selectedIndex = theForm.{1}.selectedIndex;", shippingClientIDs["State"], billingClientIDs["State"]);
            s.AppendFormat("	theForm.{0}.value = theForm.{1}.value;", shippingClientIDs["Zip"], billingClientIDs["Zip"]);
            s.AppendFormat("	theForm.{0}.selectedIndex = theForm.{1}.selectedIndex;", shippingClientIDs["Country"], billingClientIDs["Country"]);
            s.Append(" }");
            s.Append("return (true); }\n\n");

            s.Append("function TermsChecked(){ ");
            s.Append("	return (true); }");
            s.Append("</script>\n");


            Page.ClientScript.RegisterClientScriptBlock(GetType(), Guid.NewGuid().ToString(), s.ToString(), false);
        }

        private Dictionary<string, string> GetAddressChildsClientID(AddressControl addrControl)
        {
            Dictionary<string, string> clientIDs = new Dictionary<string, string>();

            TextBox txtNickName = addrControl.FindControl("NickName") as TextBox;
            TextBox txtFirstName = addrControl.FindControl("FirstName") as TextBox;
            TextBox txtLastName = addrControl.FindControl("LastName") as TextBox;
            TextBox txtAddress1 = addrControl.FindControl("Address1") as TextBox;
            TextBox txtAddress2 = addrControl.FindControl("Address2") as TextBox;
            TextBox txtCity = addrControl.FindControl("City") as TextBox;
            TextBox txtZip = addrControl.FindControl("Zip") as TextBox;
            TextBox txtCompany = addrControl.FindControl("Company") as TextBox;
            TextBox txtPhoneNumber = addrControl.FindControl("Phone") as TextBox;
            TextBox txtSuite = addrControl.FindControl("Suite") as TextBox;

            DropDownList cboAddressType = addrControl.FindControl("ResidenceType") as DropDownList;
            DropDownList cboCountry = addrControl.FindControl("Country") as DropDownList;
            DropDownList cboState = addrControl.FindControl("State") as DropDownList;

            clientIDs.Add("NickName", txtNickName.ClientID);
            clientIDs.Add("FirstName", txtFirstName.ClientID);
            clientIDs.Add("LastName", txtLastName.ClientID);
            clientIDs.Add("Address1", txtAddress1.ClientID);
            clientIDs.Add("Address2", txtAddress2.ClientID);
            clientIDs.Add("City", txtCity.ClientID);
            clientIDs.Add("Zip", txtZip.ClientID);
            clientIDs.Add("Company", txtCompany.ClientID);
            clientIDs.Add("Suite", txtSuite.ClientID);
            clientIDs.Add("Phone", txtPhoneNumber.ClientID);
            clientIDs.Add("AddressType", cboAddressType.ClientID);
            clientIDs.Add("Country", cboCountry.ClientID);
            clientIDs.Add("State", cboState.ClientID);

            return clientIDs;
        }

        private void ProcessCheckout()
        {
            SetPasswordFields();

            if (SkipRegistration)
            {
                Page.Validate("skipreg");
            }
            else
            {
                Page.Validate("registration");
            }
            Page.Validate("BillingCheckout1");
            if (AllowShipToDifferentThanBillTo)
            {
                Page.Validate("ShippingCheckout1");
            }

            bool accountOK = ProcessAccount();
            bool shippingOK = ProcessShipping(ref cart);

            if (cart.HasCoupon())
            {
                string CouponName = cart.Coupon.CouponCode;
                cart.ClearCoupon();
                cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
                cart.SetCoupon(CouponName, true);
            }

            if (accountOK && shippingOK)
            {
                if (cart.SubTotal(true, false, false, true) == Decimal.Zero)
                {
                    BillingAddress.PaymentMethodLastUsed = "CREDITCARD";
                    BillingAddress.UpdateDB();
                    Response.Redirect("~/checkoutreview.aspx?paymentmethod=CREDITCARD");
                }
                if (ctrlPaymentMethod.CREDITCARDChecked)
                {
                    if (GWActual != null && !String.IsNullOrEmpty(GWActual.ProcessingPageRedirect()))
                    {
                        Response.Redirect(GWActual.ProcessingPageRedirect());
                    }
                    else
                    {
                        Page.Validate("creditcard");

                        if (ctrlCreditCardPanel.CreditCardType == "address.cs.32".StringResource())
                        {
                            pnlCCTypeErrorMsg.Visible = true;
                        }
                        else { pnlCCTypeErrorMsg.Visible = false; }
                        if (ctrlCreditCardPanel.CardExpMonth == "address.cs.34".StringResource() || ctrlCreditCardPanel.CardExpYr == "address.cs.35".StringResource())
                        {
                            pnlCCExpDtErrorMsg.Visible = true;
                        }
                        else { pnlCCExpDtErrorMsg.Visible = false; }

                        if (Page.IsValid && !(pnlCCTypeErrorMsg.Visible || pnlCCExpDtErrorMsg.Visible))
                        {
                            ProcessPayment(AppLogic.ro_PMCreditCard);
                        }
                    }
                }
                else if (ctrlPaymentMethod.PURCHASEORDERChecked)
                {
                    ProcessPayment(AppLogic.ro_PMPurchaseOrder);
                }
                else if (ctrlPaymentMethod.CODMONEYORDERChecked)
                {
                    ProcessPayment(AppLogic.ro_PMCODMoneyOrder);
                }
                else if (ctrlPaymentMethod.CODCOMPANYCHECKChecked)
                {
                    ProcessPayment(AppLogic.ro_PMCODCompanyCheck);
                }
                else if (ctrlPaymentMethod.CODNET30Checked)
                {
                    ProcessPayment(AppLogic.ro_PMCODNet30);
                }
                else if (ctrlPaymentMethod.PAYPALChecked)
                {
                    Response.Redirect("~/paypalpane.aspx");
                }
                else if (ctrlPaymentMethod.REQUESTQUOTEChecked)
                {
                    ProcessPayment(AppLogic.ro_PMRequestQuote);
                }
                else if (ctrlPaymentMethod.CHECKBYMAILChecked)
                {
                    ProcessPayment(AppLogic.ro_PMCheckByMail);
                }
                else if (ctrlPaymentMethod.CODChecked)
                {
                    ProcessPayment(AppLogic.ro_PMCOD);
                }
                else if (ctrlPaymentMethod.ECHECKChecked)
                {
                    Page.Validate("echeck");
                    if (Page.IsValid)
                    {
                        ProcessPayment(AppLogic.ro_PMECheck);
                    }
                }
                else if (ctrlPaymentMethod.CARDINALMYECHECKChecked)
                {
                    ProcessPayment(AppLogic.ro_PMCardinalMyECheck);
                }
                else if (ctrlPaymentMethod.MICROPAYChecked)
                {
                    ProcessPayment(AppLogic.ro_PMMicropay);
                }
                else if (ctrlPaymentMethod.PAYPALEXPRESSChecked)
                {
                    ProcessPayment(AppLogic.ro_PMPayPalExpressMark);
                }                
            }

            if (cart.HasCoupon())
            {
                string CouponName = cart.Coupon.CouponCode;
                cart.ClearCoupon();
                cart.SetCoupon(CouponName, true);
                cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            }

            InitializePageContent();
            GetJavaScriptFunctions();
        }

        private void AccountInfoFields()
        {
            if (ViewState["fname"] == null)
            {
                ViewState["fname"] = "";
            }
            if (FirstName.Text.Trim() != "")
            {
                ViewState["fname"] = FirstName.Text;
                FirstName.Attributes.Add("value", FirstName.Text);
            }

            if (ViewState["custpwd2"] == null)
            {
                ViewState["custpwd2"] = "";
            }
            if (password2.Text != "")
            {
                ViewState["lname"] = LastName.Text;
            }
        }

        private void SetPasswordFields()
        {
            if (ViewState["custpwd"] == null)
            {
                ViewState["custpwd"] = "";
            }
            if (password.Text.Trim() != "" && Regex.IsMatch(password.Text.Trim(), "[^\xFF]", RegexOptions.Compiled))
            {
                ViewState["custpwd"] = password.Text;
                string fillpwd = new string('\xFF', password.Text.Length);
                password.Attributes.Add("value", fillpwd);
            }

            if (ViewState["custpwd2"] == null)
            {
                ViewState["custpwd2"] = "";
            }
            if (password2.Text != "" && Regex.IsMatch(password2.Text.Trim(), "[^\xFF]", RegexOptions.Compiled))
            {
                ViewState["custpwd2"] = password2.Text;
                string fillpwd2 = new string('\xFF', password2.Text.Length);
                password2.Attributes.Add("value", fillpwd2);
            }
        }

        private bool ShippingIsValid()
        {
            bool isValid = true;
            Page.Validate("ShippingCheckout1");
            isValid = Page.IsValid;
            return isValid;
        }

        private bool BillingIsValid()
        {
            bool isValid = true;
            Page.Validate("BillingCheckout1");
            isValid = Page.IsValid;
            return isValid;
        }

        private bool AccountIsValid()
        {
            bool acctIsValid = true;
            Page.Validate("registration");
            acctIsValid = valReqFirstName.IsValid && valReqLastName.IsValid && valAcctEmail.IsValid && valPassword.IsValid && valReqPhone.IsValid && (!valReqSecurityCode.Enabled || valReqSecurityCode.IsValid) && (!valCustSecurityCode.Enabled || valCustSecurityCode.IsValid);
            return acctIsValid;
        }

        #endregion

        #region Account Info Section

        private void InitializeAccountInfo()
        {
            if (CommonLogic.QueryStringNativeInt("errormsg") > 0)
            {
                ErrorMessage err = new ErrorMessage(CommonLogic.QueryStringNativeInt("errormsg"));
                ErrorMsgLabel.Text = Server.HtmlEncode(err.Message).Replace("+", " ");                
            }            

            if (SkipRegistration && !ThisCustomer.IsRegistered)
            {
                pnlSkipReg.Visible = true;
                tblSkipRegBox.Attributes.Add("style", AppLogic.AppConfig("BoxFrameStyle"));
                skipreg_gif.ImageUrl = AppLogic.LocateImageURL("~/App_Themes/Skin_" + SkinID.ToString() + "/images/accountinfo.gif");
                if (!AppLogic.AppConfigBool("HidePasswordFieldDuringCheckout"))
                {
                    skipRegSignin.Text = AppLogic.GetString("checkout1.aspx.8", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);
                }

                valReqSkipRegEmail.Enabled = AppLogic.AppConfigBool("AnonCheckoutReqEmail");
            }
            else if (!ThisCustomer.IsRegistered)
            {
                Signin.Text = AppLogic.GetString("checkout1.aspx.8", ThisCustomer.SkinID, ThisCustomer.LocaleSetting);

                pnlAccountInfo.Visible = true;
                tblAccount.Attributes.Add("style", "border-style: solid; border-width: 0px; border-color: #" + AppLogic.AppConfig("HeaderBGColor"));
                tblAccountBox.Attributes.Add("style", AppLogic.AppConfig("BoxFrameStyle"));
                accountinfo_gif.ImageUrl = AppLogic.LocateImageURL("~/App_Themes/Skin_" + SkinID.ToString() + "/images/accountinfo.gif");
                if (ThisCustomer.FirstName.Length > 0 || BillingAddress.FirstName.Length > 0)
                {
                    FirstName.Text = Server.HtmlEncode(CommonLogic.IIF(ThisCustomer.FirstName.Length != 0, ThisCustomer.FirstName, BillingAddress.FirstName));
                }
                if (ThisCustomer.LastName.Length > 0 || BillingAddress.LastName.Length > 0)
                {
                    LastName.Text = Server.HtmlEncode(CommonLogic.IIF(ThisCustomer.LastName.Length != 0, ThisCustomer.LastName, BillingAddress.LastName));
                }
                password.TextMode = TextBoxMode.Password;
                password2.TextMode = TextBoxMode.Password;

                if (ThisCustomer.EMail.Length > 0)
                {
                    String emailx = ThisCustomer.EMail;
                    EMail.Text = Server.HtmlEncode(emailx);
                }

                if ((AppLogic.AppConfigBool("PasswordIsOptionalDuringCheckout") && AppLogic.AppConfigBool("HidePasswordFieldDuringCheckout")))
                {
                    valPassword.Visible = false;
                    valPassword.Enabled = false;
                }

                if (ThisCustomer.Phone.Length > 0 || BillingAddress.Phone.Length > 0)
                {
                    Phone.Text = Server.HtmlEncode(CommonLogic.IIF(ThisCustomer.Phone.Length != 0, ThisCustomer.Phone, BillingAddress.Phone));
                }
                // Create a phone validation error message

                Checkout1aspx23.Text = "*" + "createaccount.aspx.23".StringResource();
                if (ThisCustomer.IsRegistered && ThisCustomer.OKToEMail)
                {
                    OKToEMailYes.Checked = true;
                }
                if (ThisCustomer.IsRegistered && !ThisCustomer.OKToEMail)
                {
                    OKToEMailNo.Checked = true;
                }

                if (AppLogic.AppConfigBool("RequireOver13Checked"))
                {
                    Over13.Visible = true;

                    if (ThisCustomer.IsRegistered)
                    {
                        Over13.Checked = ThisCustomer.IsOver13;
                    }
                }

                if (RequireSecurityCode)
                {
                    // Create a random code and store it in the Session object.
                    Session["SecurityCode"] = CommonLogic.GenerateRandomCode(6);
                    signinaspx21.Visible = true;
                    SecurityCode.Visible = true;
                    valReqSecurityCode.Visible = true;
                    valReqSecurityCode.Enabled = true;
                    valCustSecurityCode.Enabled = true;
                    valCustSecurityCode.Visible = true;
                    valCustSecurityCode.ErrorMessage = AppLogic.GetString("Checkout1_process.aspx.2", 1, Localization.GetDefaultLocale());
                    SecurityImage.Visible = true;
                    if (!IsPostBack)
                    {
                        SecurityImage.ImageUrl = "Captcha.ashx?id=1";
                    }
                    else
                    {
                        SecurityImage.ImageUrl = "Captcha.ashx?id=2";
                    }
                }
            }



        }

        private bool ProcessAccount()
        {
            string AccountName = (FirstName.Text.Trim() + " " + LastName.Text.Trim()).Trim();
            if (SkipRegistration)
            {
                //AccountName = (BillingFirstName.Text.Trim() + " " + BillingLastName.Text.Trim()).Trim();
                AccountName = string.Format("{0} {1}", ctrlBillingAddress.FirstName.Trim(), ctrlBillingAddress.LastName.Trim());
            }

            //LovelyEcom Add
            if (AppLogic.AppConfig("VerifyAddressesProvider") != "")
            {
                string VerifyResult = String.Empty;
                Address StandardizedAddress = null;
                ErrorMsgLabel.Text = "";
                Address Verifyshipping = new Address();
                Verifyshipping.Address1 = ctrlShippingAddress.Address1;
                Verifyshipping.Address2 = ctrlShippingAddress.Address2;
                Verifyshipping.City = ctrlShippingAddress.City;
                Verifyshipping.State = ctrlShippingAddress.State;
                Verifyshipping.Zip = ctrlShippingAddress.ZipCode;

                VerifyResult = AddressValidation.RunValidate(Verifyshipping, out StandardizedAddress);
                if (VerifyResult != AppLogic.ro_OK)
                {
                    ErrorMsgLabel.Text += "Shipping " + VerifyResult; //lovely Ecom Added
                    return false;
                }

                Address Verifybilling = new Address();
                Verifybilling.Address1 = ctrlBillingAddress.Address1;
                Verifybilling.Address2 = ctrlBillingAddress.Address2;
                Verifybilling.City = ctrlBillingAddress.City;
                Verifybilling.State = ctrlBillingAddress.State;
                Verifybilling.Zip = ctrlBillingAddress.ZipCode;

                VerifyResult = AddressValidation.RunValidate(Verifybilling, out StandardizedAddress);
     
                if (VerifyResult != AppLogic.ro_OK)
                {
                    ErrorMsgLabel.Text += "Billing " + VerifyResult;
                    return false;
                }

            }
            //LovelyEcom end



            if (Page.IsValid && (AccountName.Length > 0 || ThisCustomer.IsRegistered))
            {
                if (!ThisCustomer.IsRegistered)
                {
                    //check to make sure that when anonymous checkout required email is set to true, we will have to check for the value of txtSkipRegEmail
                    //otherwise Email.text
                    String EMailField = CommonLogic.IIF(SkipRegistration && !ThisCustomer.IsRegistered, txtSkipRegEmail.Text.ToLower().Trim(), EMail.Text.ToLower().Trim());
                    String PWD = ViewState["custpwd"].ToString();
                    Password p = new Password(PWD);
                    String newpwd = p.SaltedPassword;
                    Nullable<int> newsaltkey = p.Salt;
                    if (ThisCustomer.Password.Length != 0)
                    {
                        // do NOT allow passwords to be changed on this page. this is only for creating an account.
                        // if they want to change their password, they must use their account page
                        newpwd = null;
                        newsaltkey = null;
                    }
                    if (Customer.NewEmailPassesDuplicationRules(EMailField, ThisCustomer.CustomerID, SkipRegistration))
                    {
                        ThisCustomer.UpdateCustomer(
                            /*CustomerLevelID*/ null,
                                                /*EMail*/ EMailField,
                                                /*SaltedAndHashedPassword*/ newpwd,
                                                /*SaltKey*/ newsaltkey,
                                                /*DateOfBirth*/ null,
                                                /*Gender*/ null,
                                                /*FirstName*/ FirstName.Text.Trim(),
                                                /*LastName*/ LastName.Text.Trim(),
                                                /*Notes*/ null,
                                                /*SkinID*/ null,
                                                /*Phone*/ Phone.Text.Trim(),
                                                /*AffiliateID*/ null,
                                                /*Referrer*/ null,
                                                /*CouponCode*/ null,
                                                /*OkToEmail*/ CommonLogic.IIF(OKToEMailYes.Checked, 1, 0),
                                                /*IsAdmin*/ null,
                            /*BillingEqualsShipping*/ CommonLogic.IIF(AppLogic.AppConfigBool("AllowShipToDifferentThanBillTo"), 0, 1),
                                                /*LastIPAddress*/ null,
                                                /*OrderNotes*/ null,
                                                /*SubscriptionExpiresOn*/ null,
                                                /*RTShipRequest*/ null,
                                                /*RTShipResponse*/ null,
                                                /*OrderOptions*/ null,
                                                /*LocaleSetting*/ null,
                                                /*MicroPayBalance*/ null,
                                                /*RecurringShippingMethodID*/ null,
                                                /*RecurringShippingMethod*/ null,
                                                /*BillingAddressID*/ null,
                                                /*ShippingAddressID*/ null,
                                                /*GiftRegistryGUID*/ null,
                                                /*GiftRegistryIsAnonymous*/ null,
                                                /*GiftRegistryAllowSearchByOthers*/ null,
                                                /*GiftRegistryNickName*/ null,
                                                /*GiftRegistryHideShippingAddresses*/ null,
                                                /*CODCompanyCheckAllowed*/ null,
                                                /*CODNet30Allowed*/ null,
                                                /*ExtensionData*/ null,
                                                /*FinalizationData*/ null,
                                                /*Deleted*/ null,
                                                /*Over13Checked*/ CommonLogic.IIF(Over13.Checked || SkipRegOver13.Checked, 1, 0),
                                                /*CurrencySetting*/ null,
                                                /*VATSetting*/ null,
                                                /*VATRegistrationID*/ null,
                                                /*StoreCCInDB*/ null,
                                                /*IsRegistered*/ CommonLogic.IIF(SkipRegistration, 0, 1),
                                                /*LockedUntil*/ null,
                                                /*AdminCanViewCC*/ null,
                                                /*BadLogin*/ null,
                                                /*Active*/ null,
                                                /*PwdChangeRequired*/ null,
                                                /*RegisterDate*/ null,
                                                /*StoreId*/AppLogic.StoreID()
                            );


                        BillingAddress = new Address();
                        if (ThisCustomer.PrimaryBillingAddressID == 0)
                        {
                            BillingAddress.LastName = ctrlBillingAddress.LastName;
                            BillingAddress.FirstName = ctrlBillingAddress.FirstName;
                            BillingAddress.Phone = ctrlBillingAddress.PhoneNumber;
                            BillingAddress.Company = ctrlBillingAddress.Company;
                            BillingAddress.ResidenceType = (ResidenceTypes)Enum.Parse(typeof(ResidenceTypes), ctrlBillingAddress.ResidenceType, true);//(ResidenceTypes) Convert.ToInt32( ctrlBillingAddress.AddressType);
                            BillingAddress.Address1 = ctrlBillingAddress.Address1;
                            BillingAddress.Address2 = ctrlBillingAddress.Address2;
                            BillingAddress.Suite = ctrlBillingAddress.Suite;
                            BillingAddress.City = ctrlBillingAddress.City;
                            BillingAddress.State = ctrlBillingAddress.State;
                            BillingAddress.Zip = ctrlBillingAddress.ZipCode;
                            BillingAddress.Country = ctrlBillingAddress.Country;
                            BillingAddress.EMail = EMailField;

                            BillingAddress.InsertDB(ThisCustomer.CustomerID);
                            BillingAddress.MakeCustomersPrimaryAddress(AddressTypes.Billing);
                            ThisCustomer.PrimaryBillingAddressID = BillingAddress.AddressID;
                        }

                        if (AllowShipToDifferentThanBillTo && !AppLogic.AppConfigBool("SkipShippingOnCheckout"))
                        {
                            ShippingAddress = new Address();
                            if (ThisCustomer.PrimaryShippingAddressID == 0)
                            {
                                ShippingAddress.LastName = ctrlShippingAddress.LastName;
                                ShippingAddress.FirstName = ctrlShippingAddress.FirstName;
                                ShippingAddress.Phone = ctrlShippingAddress.PhoneNumber;
                                ShippingAddress.Company = ctrlShippingAddress.Company;
                                ShippingAddress.ResidenceType = (ResidenceTypes)Enum.Parse(typeof(ResidenceTypes), ctrlShippingAddress.ResidenceType, true);//(ResidenceTypes)Convert.ToInt32(ctrlShippingAddress.AddressType);
                                ShippingAddress.Address1 = ctrlShippingAddress.Address1;
                                ShippingAddress.Address2 = ctrlShippingAddress.Address2;
                                ShippingAddress.Suite = ctrlShippingAddress.Suite;
                                ShippingAddress.City = ctrlShippingAddress.City;
                                ShippingAddress.State = ctrlShippingAddress.State;
                                ShippingAddress.Zip = ctrlShippingAddress.ZipCode;
                                ShippingAddress.Country = ctrlShippingAddress.Country;
                                ShippingAddress.EMail = EMailField;

                                ShippingAddress.InsertDB(ThisCustomer.CustomerID);
                                ShippingAddress.MakeCustomersPrimaryAddress(AddressTypes.Shipping);
                                ThisCustomer.PrimaryShippingAddressID = ShippingAddress.AddressID;
                            }
                        }
                        else
                        {
                            BillingAddress.MakeCustomersPrimaryAddress(AddressTypes.Shipping);
                        }

                        if (AppLogic.AppConfigBool("SendWelcomeEmail") && EMailField.IndexOf("@") != -1 && ThisCustomer.IsRegistered == true)
                        {
                            // don't let a simple welcome stop checkout!
                            try
                            {
                                AppLogic.SendMail("createaccount.aspx.79".StringResource(), AppLogic.RunXmlPackage(AppLogic.AppConfig("XmlPackage.WelcomeEmail"), null, ThisCustomer, SkinID, "", "fullname=" + FirstName.Text.Trim() + " " + LastName.Text.Trim(), false, false, EntityHelpers), true, AppLogic.AppConfig("MailMe_FromAddress"), AppLogic.AppConfig("MailMe_FromName"), EMailField, FirstName.Text.Trim() + " " + LastName.Text.Trim(), "", AppLogic.AppConfig("MailMe_Server"));
                            }
                            catch { }
                        }
                    }
                    else
                    {
                        ErrorMsgLabel.Text = AppLogic.GetString("createaccount_process.aspx.1", 1, Localization.GetDefaultLocale());
                        return false;
                    }
                }
            }
            else
            {
                ErrorMsgLabel.Text += "<br /><br /> " + AppLogic.GetString("checkout1.aspx.9", 1, Localization.GetDefaultLocale()) + "<br /><br />";
                if (AccountName.Length == 0)
                {
                    ErrorMsgLabel.Text += "&bull; " + AppLogic.GetString("createaccount.aspx.5", 1, Localization.GetDefaultLocale()) + "<br />";
                }
                foreach (IValidator aValidator in Validators)
                {
                    if (!aValidator.IsValid)
                    {
                        ErrorMsgLabel.Text += "&bull; " + aValidator.ErrorMessage + "<br />";
                    }
                }
                ErrorMsgLabel.Text += "<br />";
                return false;
            }

            if (AppLogic.AppConfigBool("DynamicRelatedProducts.Enabled") || AppLogic.AppConfigBool("RecentlyViewedProducts.Enabled"))
            {                
                ThisCustomer.ReplaceProductViewFromAnonymous();
            }

            return true;
        }

        #endregion

        #region Shipping Options Section

        private void InitializeShippingOptions(ref ShoppingCart cart)
        {
            if (ThisCustomer.PrimaryShippingAddressID > 0)
            {
                divShippingOptions.Visible = true;
                divRecalcShipping.Visible = false;
                btnCheckOut.Enabled = true;
              
            }
            else
            {
                divShippingOptions.Visible = false;
                divRecalcShipping.Visible = true;
                btnCheckOut.Enabled = false;
            }

            if (AppLogic.AppConfigBool("SkipShippingOnCheckout") || cart.IsAllDownloadComponents() || cart.IsAllSystemComponents() || cart.NoShippingRequiredComponents() || cart.IsAllEmailGiftCards())
            {
                trShippingOptions.Visible = false;
                btnCheckOut.Enabled = true;
                return;
            }
            else
            {
                trShippingOptions.Visible = true;
            }          

            trShippingOptions.Visible = cart.CartAllowsShippingMethodSelection;
            ctrlShippingMethods.Visible = cart.CartAllowsShippingMethodSelection;
            
            if ((!cart.CartAllowsShippingMethodSelection || AnyShippingMethodsFound) || (!AnyShippingMethodsFound && !AppLogic.AppConfigBool("FreeShippingAllowsRateSelection") && cart.ShippingIsFree))
            {
                btnCheckOut.Visible = true;
            }

            if (cart.CartAllowsShippingMethodSelection)
            {
                ctrlShippingMethods.HeaderText = string.Empty;
                if (Shipping.MultiShipEnabled() && cart.TotalQuantity() > 1)
                {
                    ctrlShippingMethods.HeaderText = "<p><b>" + String.Format("checkoutshipping.aspx.15".StringResource(), "checkoutshippingmult.aspx") + "</b></p>";
                }

                if (!AppLogic.AppConfigBool("FreeShippingAllowsRateSelection") && (cart.IsAllFreeShippingComponents() || (!AnyShippingMethodsFound && cart.ShippingIsFree) || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CustomerLevelHasFreeShipping || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.ExceedsFreeShippingThreshold || cart.FreeShippingReason == Shipping.FreeShippingReasonEnum.CouponHasFreeShipping))
                {
                    ctrlShippingMethods.HeaderText += "<p><b>" + cart.GetFreeShippingReason() + "</b></p>";
                }
                else
                {
                    if (ThisCustomer.PrimaryShippingAddressID > 0)
                    {
                        ctrlShippingMethods.HeaderText += "<p><b>" + "checkoutshipping.aspx.11".StringResource() + "</b></p>";
                    }
                    else
                    {
                        lblRecalcShippingMsg.Text = string.Format("<b>{0}</b>", "checkout1.aspx.6".StringResource());
                    }

                    if (AppLogic.AppConfigBool("Checkout.UseOnePageCheckout.UseFinalReviewOrderPage"))
                    {                        
                        btnCheckOut.Text = "checkoutpayment.aspx.16".StringResource();
                    }
                    else
                    {
                        btnCheckOut.Text = "checkout1.aspx.1".StringResource();
                    }
                }
            }

            PopulateShippingMethods();

            if ((AppLogic.AppConfigBool("RTShipping.DumpXMLOnCheckoutShippingPage") || AppLogic.AppConfigBool("RTShipping.DumpXMLOnCartPage")) && cart.ShipCalcID == Shipping.ShippingCalculationEnum.UseRealTimeRates)
            {
                StringBuilder tmpS = new StringBuilder(4096);
                tmpS.Append("<hr break=\"all\"/>");

                using (SqlConnection con = new SqlConnection(DB.GetDBConn()))
                {
                    con.Open();
                    using (IDataReader rs = DB.GetRS("Select RTShipRequest,RTShipResponse from customer  with (NOLOCK)  where CustomerID=" + ThisCustomer.CustomerID.ToString(), con))
                    {
                        if (rs.Read())
                        {
                            String s = DB.RSField(rs, "RTShipRequest");
                            s = s.Replace("<?xml version=\"1.0\"?>", "");
                            try
                            {
                                s = XmlCommon.PrettyPrintXml("<roottag_justaddedfordisplayonthispage>" + s + "</roottag_justaddedfordisplayonthispage>"); // the RTShipRequest may have "two" XML docs in it :)
                            }
                            catch
                            {
                                s = DB.RSField(rs, "RTShipRequest");
                            }
                            tmpS.Append("<b>" + "shoppingcart.aspx.5".StringResource()+ "</b><br/><textarea rows=60 style=\"width: 100%\">" + s + "</textarea><br/><br/>");
                            try
                            {
                                s = XmlCommon.PrettyPrintXml(DB.RSField(rs, "RTShipResponse"));
                            }
                            catch
                            {
                                s = DB.RSField(rs, "RTShipResponse");
                            }
                            tmpS.Append("<b>" + "shoppingcart.aspx.6".StringResource() + "</b><br/><textarea rows=60 style=\"width: 100%\">" + s + "</textarea><br/><br/>");
                        }
                    }
                }

                DebugInfo.Text = tmpS.ToString();
            }
        }

        private bool ProcessShipping(ref ShoppingCart cart)
        {
            if (AppLogic.AppConfigBool("SkipShippingOnCheckout") || cart.IsAllDownloadComponents() || cart.IsAllSystemComponents())
            {
                return true;
            }

            String ShippingMethodIDFormField = string.Empty;
            bool hasSelected = ctrlShippingMethods.SelectedItem != null;

            if (hasSelected)
            {
                ShippingMethodIDFormField = ctrlShippingMethods.SelectedItem.Value;
            }

            if (ShippingMethodIDFormField.Length == 0 && (!cart.ShippingIsFree || (ctrlShippingMethods.DataSource != null && ctrlShippingMethods.DataSource.Count > 0 ) ))
            {
                pnlErrorMsg.Visible = true;
                ErrorMsgLabel.Text = "checkoutshipping.aspx.17".StringResource();
                return false;
            }
            else
            {
                if (cart.IsEmpty())
                {
                    Response.Redirect("~/shoppingcart.aspx");
                }

                int ShippingMethodID = 0;
                String ShippingMethod = String.Empty;
                if (cart.ShipCalcID != Shipping.ShippingCalculationEnum.UseRealTimeRates)
                {
                    ShippingMethodID = Localization.ParseUSInt(ShippingMethodIDFormField);
                    ShippingMethod = Shipping.GetShippingMethodName(ShippingMethodID, null);
                }
                else
                {
                    if (ShippingMethodIDFormField.Length != 0 && ShippingMethodIDFormField.IndexOf('|') != -1)
                    {
                        String[] frmsplit = ShippingMethodIDFormField.Split('|');
                        ShippingMethodID = Localization.ParseUSInt(frmsplit[0]);
                        ShippingMethod = String.Format("{0}|{1}", frmsplit[1], frmsplit[2]);
                    }
                }

                if (cart.ShippingIsFree && !AppLogic.AppConfigBool("FreeShippingAllowsRateSelection"))
                {
                    ShippingMethodID = 0;
                    String cartFreeShippingReason = cart.GetFreeShippingReason();
                    ShippingMethod = string.Format("shoppingcart.aspx.16".StringResource() + " : {0}", cartFreeShippingReason);
                }

                String sql = String.Format("update ShoppingCart set ShippingMethodID={0}, ShippingMethod={1} where CustomerID={2} and CartType={3}", ShippingMethodID.ToString(), DB.SQuote(ShippingMethod), ThisCustomer.CustomerID.ToString(), ((int) CartTypeEnum.ShoppingCart).ToString());
                DB.ExecuteSQL(sql);
                cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
                CartTotal = cart.Total(true);
                NetTotal = CartTotal - CommonLogic.IIF(cart.Coupon.CouponType == CouponTypeEnum.GiftCard, CommonLogic.IIF(CartTotal < cart.Coupon.DiscountAmount, CartTotal, cart.Coupon.DiscountAmount), 0);
                if (cart.ContainsGiftCard())
                {
                    Response.Redirect("~/checkoutgiftcard.aspx");
                }
            }
            return true;
        }

        #endregion

        #region Payment Options Section

        private void InitializePaymentOptions(ref ShoppingCart cart)
        {
            JSPopupRoutines.Text = AppLogic.GetJSPopupRoutines();

            //HERE WE WILL DO THE LOOKUP for the new supported Shipping2Payment mapping
            if (AppLogic.AppConfigBool("UseMappingShipToPayment"))
            {
                try
                {
                    int intCustomerSelectedShippingMethodID = cart.FirstItem().ShippingMethodID;
                    
                    using (SqlConnection con = new SqlConnection(DB.GetDBConn()))
                    {
                        con.Open();
                        using (IDataReader rsReferencePMForSelectedShippingMethod = DB.GetRS("SELECT MappedPM FROM ShippingMethod WHERE ShippingMethodID=" + intCustomerSelectedShippingMethodID.ToString(), con))
                        {
                            while (rsReferencePMForSelectedShippingMethod.Read())
                            {
                                AllowedPaymentMethods = DB.RSField(rsReferencePMForSelectedShippingMethod, "MappedPM").ToUpperInvariant();
                            }
                        }
                    }

                    if (AllowedPaymentMethods.Length <= 0)
                    {
                        AllowedPaymentMethods = AppLogic.AppConfig("PaymentMethods").ToUpperInvariant();
                    }
                }
                catch
                {
                    AllowedPaymentMethods = AppLogic.AppConfig("PaymentMethods").ToUpperInvariant();
                }
            }
            else
            {
                AllowedPaymentMethods = AppLogic.AppConfig("PaymentMethods").ToUpperInvariant();

                if (AppLogic.MicropayIsEnabled() &&
                    !cart.HasSystemComponents())
                {
                    if (AllowedPaymentMethods.Length != 0)
                    {
                        AllowedPaymentMethods += ",";
                    }
                    AllowedPaymentMethods += AppLogic.ro_PMMicropay;
                }
            }

            // When PAYPALPRO is active Gateway or PAYPALEXPRESS is available Payment Method
            // then we want to make the PayPal Express Mark available
            if ((AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWPAYPALPRO || AllowedPaymentMethods.IndexOf(AppLogic.ro_PMPayPalExpress) > -1)
                &&
                AllowedPaymentMethods.IndexOf(AppLogic.ro_PMPayPalExpressMark) == -1)
            {
                if (AllowedPaymentMethods.Length != 0)
                {
                    AllowedPaymentMethods += ",";
                }
                AllowedPaymentMethods += AppLogic.ro_PMPayPalExpressMark;
            }

            // Need to dbl check this
            SelectedPaymentType = CommonLogic.IIF(SelectedPaymentType == "" && ThisCustomer.RequestedPaymentMethod != "" && AllowedPaymentMethods.IndexOf(ThisCustomer.RequestedPaymentMethod, StringComparison.InvariantCultureIgnoreCase) != -1, AppLogic.CleanPaymentMethod(ThisCustomer.RequestedPaymentMethod), ViewState["SelectedPaymentType"].ToString());

            //Set credit card pane to be visible if that payment method is allowed, and no other payment method
            // is trying to be shown: If UseMappingShipToPayment is not activated Credit Card will always be
            // the default payment option that shows expnande to the customer.
            if (AppLogic.AppConfigBool("UseMappingShipToPayment"))
            {
                string[] strSplittedCurrentMappingsInDB = AllowedPaymentMethods.Split(new char[] {','});

                String PM = AppLogic.CleanPaymentMethod(strSplittedCurrentMappingsInDB[0]);
                if (PM == AppLogic.ro_PMMicropay)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMMicropay) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMMicropay;
                        ctrlPaymentMethod.MICROPAYChecked = true;//pmtMICROPAY.Checked = true;
                        ctrlPaymentMethod.ShowMICROPAY = true;//pnlMicroPayPane.Visible = true;
                    }
                }
                else if (PM == AppLogic.ro_PMPurchaseOrder)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMPurchaseOrder) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMPurchaseOrder;
                        ctrlPaymentMethod.PURCHASEORDERChecked = true;//pmtPURCHASEORDER.Checked = true;
                        ctrlPaymentMethod.ShowPURCHASEORDER = true;//pnlPOPane.Visible = true;
                    }
                }
                else if (PM == AppLogic.ro_PMCreditCard)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMCreditCard) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMCreditCard;
                        ctrlPaymentMethod.CREDITCARDChecked = true;//pmtCreditCard.Checked = true;
                        ctrlPaymentMethod.ShowCREDITCARD = true;//pnlCreditCardPane.Visible = true;
                    }
                }
                else if (PM == AppLogic.ro_PMPayPal)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMPayPal) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMPayPal;
                        ctrlPaymentMethod.PAYPALChecked = true;//pmtPAYPAL.Checked = true;
                        ctrlPaymentMethod.ShowPAYPAL = true;//pnlPayPalPane.Visible = true;
                    }
                }
                else if (PM == AppLogic.ro_PMPayPalExpress)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMPayPalExpress) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMPayPalExpress;
                        ctrlPaymentMethod.PAYPALEXPRESSChecked = true;//pmtPAYPALEXPRESS.Checked = true;
                        ctrlPaymentMethod.ShowPAYPALEXPRESS = true;//pnlPayPalExpressPane.Visible = true;
                    }
                }
                else if (PM == AppLogic.ro_PMCOD)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMCOD) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMCOD;
                        ctrlPaymentMethod.CODChecked = true;//pmtCOD.Checked = true;
                        ctrlPaymentMethod.ShowCOD = true;//pnlCODPane.Visible = true;
                    }
                }

                else if (PM == AppLogic.ro_PMECheck)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMECheck) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMECheck;
                        ctrlPaymentMethod.ECHECKChecked = true;//pmtECHECK.Checked = true;
                        ctrlPaymentMethod.ShowECHECK = true;//pnlEcheckPane.Visible = true;
                    }
                }

                else if (PM == AppLogic.ro_PMCheckByMail)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMCheckByMail) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMCheckByMail;
                        ctrlPaymentMethod.CHECKBYMAILChecked = true;//pmtCHECKBYMAIL.Checked = true;
                        ctrlPaymentMethod.ShowCHECKBYMAIL = true;//pnlCheckByMailPane.Visible = true;
                    }
                }

                else if (PM == AppLogic.ro_PMRequestQuote)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMRequestQuote) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMRequestQuote;
                        ctrlPaymentMethod.REQUESTQUOTEChecked = true;//pmtREQUESTQUOTE.Checked = true;
                        ctrlPaymentMethod.ShowREQUESTQUOTE = true;//pnlReqQuotePane.Visible = true;
                    }
                }


                else if (PM == AppLogic.ro_PMCODNet30)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMCODNet30) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMCODNet30;
                        ctrlPaymentMethod.CODNET30Checked = true;//pmtCODNET30.Checked = true;
                        ctrlPaymentMethod.ShowCODNET30 = true;//pnlCODNet30Pane.Visible = true;
                    }
                }

                else if (PM == AppLogic.ro_PMCODCompanyCheck)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMCODCompanyCheck) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMCODCompanyCheck;
                        ctrlPaymentMethod.CODCOMPANYCHECKChecked = true;//pmtCODCOMPANYCHECK.Checked = true;
                        ctrlPaymentMethod.ShowCODCOMPANYCHECK = true;//pnlCODCoCheckPane.Visible = true;
                    }
                }

                else if (PM == AppLogic.ro_PMCODMoneyOrder)
                {
                    if (SelectedPaymentType.Length == 0 &&
                        AllowedPaymentMethods.IndexOf(AppLogic.ro_PMCODMoneyOrder) != -1)
                    {
                        ResetPaymentPanes();
                        SelectedPaymentType = AppLogic.ro_PMCODMoneyOrder;
                        ctrlPaymentMethod.CODMONEYORDERChecked = true;//pmtCODMONEYORDER.Checked = true;
                        ctrlPaymentMethod.ShowCODMONEYORDER = true;//pnlCODMOPane.Visible = true;
                    }
                }
            }


            String TransactionMode = AppLogic.AppConfig("TransactionMode").Trim().ToUpperInvariant();
            bool useLiveTransactions = AppLogic.AppConfigBool("UseLiveTransactions");

            StringBuilder OrderFinalizationInstructions = new StringBuilder(4096);
            String OrderFinalizationXmlPackageName = AppLogic.AppConfig("XmlPackage.OrderFinalization");
            String OrderFinalizationXmlPackageFN = Server.MapPath("xmlpackages/" + OrderFinalizationXmlPackageName);

            if (CommonLogic.FileExists(OrderFinalizationXmlPackageFN))
            {
                OrderFinalizationInstructions.Append("<p align=\"left\"><b>" + "checkoutreview.aspx.24".StringResource() + "</b></p>");
                OrderFinalizationInstructions.Append(AppLogic.RunXmlPackage(OrderFinalizationXmlPackageName, null, ThisCustomer, SkinID, string.Empty, string.Empty, false, false));
            }
            if (OrderFinalizationInstructions.Length != 0)
            {
                OrderFinalizationInstructions.Append("<br/>");
            }
            Finalization.Text = OrderFinalizationInstructions.ToString(); // set the no payment panel here, in case it is needed

            if (CommonLogic.QueryStringNativeInt("ErrorMsg") > 0)
            {
                pnlErrorMsg.Visible = true;
                ErrorMessage err = new ErrorMessage(CommonLogic.QueryStringNativeInt("ErrorMsg"));
                ErrorMsgLabel.Text = Server.HtmlEncode(err.Message).Replace("+", " ");
            }

            String XmlPackageName = AppLogic.AppConfig("XmlPackage.CheckoutPaymentPageHeader");
            if (XmlPackageName.Length != 0)
            {
                XmlPackage_CheckoutPaymentPageHeader.Text = AppLogic.RunXmlPackage(XmlPackageName, base.GetParser, ThisCustomer, SkinID, String.Empty, String.Empty, true, true);
            }

            if(NetTotal < 0)
            { NetTotal = Decimal.Zero; }
           
            if (NetTotal == Decimal.Zero && AppLogic.AppConfigBool("SkipPaymentEntryOnZeroDollarCheckout"))
            {
                NoPaymentRequired.Text = "checkoutpayment.aspx.28".StringResource();
                pnlNoPaymentRequired.Visible = true;
                pnlPaymentOptions.Visible = false;
                paymentPanes.Visible = false;
            }
            else
            {
                NoPaymentRequired.Text = "";
                pnlNoPaymentRequired.Visible = false;
                pnlPaymentOptions.Visible = true;
                paymentPanes.Visible = true;
            }

            WritePaymentPanels();
            pnlRequireTerms.Visible = RequireTerms;
            
        }


        private void ProcessPayment(string PaymentMethod)
        {
            ErrorMessage err;
            if (!TermsAndConditionsAccepted)
            {
                err = new ErrorMessage(Server.HtmlEncode("Checkoutpayment.aspx.15".StringResource()));
                Response.Redirect( "~/Checkout1.aspx?errormsg=" + err.MessageId);
            }
            int OrderNumber = 0;

            if (NetTotal == Decimal.Zero && AppLogic.AppConfigBool("SkipPaymentEntryOnZeroDollarCheckout"))
            {                
                PaymentMethod = "Credit Card";
            }

            AppLogic.ValidatePM(PaymentMethod); // this WILL throw a hard security exception on any problem!

            if (!ThisCustomer.IsRegistered)
            {
                bool boolAllowAnon = AppLogic.AppConfigBool("PasswordIsOptionalDuringCheckout") && AppLogic.AppConfigBool("HidePasswordFieldDuringCheckout");

                if (!boolAllowAnon && (PaymentMethod == AppLogic.ro_PMPayPalExpress || PaymentMethod == AppLogic.ro_PMPayPalExpressMark))
                {
                    boolAllowAnon = AppLogic.AppConfigBool("PayPal.Express.AllowAnonCheckout");
                }

                if (!boolAllowAnon)
                {
                    Response.Redirect("~/createaccount.aspx?checkout=true");
                }
            }

            if (cart.IsEmpty())
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1");
            }

            if (cart.InventoryTrimmed)
            {
                err = new ErrorMessage(Server.HtmlEncode("shoppingcart.aspx.3".StringResource()));
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&errormsg=" + err.MessageId);
            }

            if (cart.RecurringScheduleConflict)
            {
                err = new ErrorMessage(Server.HtmlEncode("shoppingcart.aspx.19".StringResource()));
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&errormsg=" + err.MessageId);
            }

            if (cart.HasCoupon() &&
                !cart.CouponIsValid)
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&discountvalid=false");
            }

            if (!cart.MeetsMinimumOrderAmount(AppLogic.AppConfigUSDecimal("CartMinOrderAmount")))
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1");
            }

            if (!cart.MeetsMinimumOrderQuantity(AppLogic.AppConfigUSInt("MinCartItemsBeforeCheckout")))
            {
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1");
            }

            // re-validate all shipping info, as ANYTHING could have changed since last page:
            if (!cart.ShippingIsAllValid())
            {
                err = new ErrorMessage(Server.HtmlEncode("shoppingcart.cs.95".StringResource()));
                HttpContext.Current.Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&errormsg=" + err.MessageId);
            }

            Address BillingAddress = new Address();
            BillingAddress.LoadByCustomer(ThisCustomer.CustomerID, ThisCustomer.PrimaryBillingAddressID, AddressTypes.Billing);

            if (ThisCustomer.PrimaryBillingAddressID == 0 || (ThisCustomer.PrimaryShippingAddressID == 0 && !AppLogic.AppConfigBool("SkipShippingOnCheckout") && !cart.IsAllDownloadComponents() && !cart.IsAllSystemComponents()))
            {
                err = new ErrorMessage(Server.HtmlEncode("checkoutpayment.aspx.2".StringResource()));
                Response.Redirect("~/shoppingcart.aspx?resetlinkback=1&errormsg=" + err.MessageId);
            }

            // ----------------------------------------------------------------
            // Get the finalization info (if any):
            // ----------------------------------------------------------------
            StringBuilder FinalizationXml = new StringBuilder(4096);
            FinalizationXml.Append("<root>");
            for (int i = 0; i < Request.Form.Count; i++)
            {
                String FieldName = Request.Form.Keys[i];
                String FieldVal = Request.Form[Request.Form.Keys[i]].Trim();
                if (FieldName.StartsWith("finalization", StringComparison.InvariantCultureIgnoreCase) &&
                    !FieldName.EndsWith("_vldt", StringComparison.InvariantCultureIgnoreCase))
                {
                    FinalizationXml.Append("<field>");
                    FinalizationXml.Append("<" + XmlCommon.XmlEncode(FieldName) + ">");
                    FinalizationXml.Append(XmlCommon.XmlEncode(FieldVal));
                    FinalizationXml.Append("</" + XmlCommon.XmlEncode(FieldName) + ">");
                    FinalizationXml.Append("</field>");
                }
            }
            FinalizationXml.Append("</root>");
            DB.ExecuteSQL(String.Format("update customer set FinalizationData={0} where CustomerID={1}", DB.SQuote(FinalizationXml.ToString()), ThisCustomer.CustomerID.ToString()));

            // ----------------------------------------------------------------
            // Store the payment info (if required):
            // ----------------------------------------------------------------
            if (PaymentMethod.Length == 0)
            {
                pnlErrorMsg.Visible = true;
                ErrorMsgLabel.Text = "checkoutpayment.aspx.20".StringResource();
                return;
            }
            String PM = AppLogic.CleanPaymentMethod(PaymentMethod);
            if (PM == AppLogic.ro_PMCreditCard)
            {
                String CardName = ctrlCreditCardPanel.CreditCardName;
                String CardNumber = ctrlCreditCardPanel.CreditCardNumber.Trim().Replace(" ", "");
                String CardExtraCode = ctrlCreditCardPanel.CreditCardVerCd.Trim().Replace(" ", "");
                String strCardType = ctrlCreditCardPanel.CreditCardType.Trim();
                String CardExpirationMonth = ctrlCreditCardPanel.CardExpMonth.Trim().Replace(" ", "");
                String CardExpirationYear = ctrlCreditCardPanel.CardExpYr.Trim().Replace(" ", "");

				String CardStartDate = String.Empty;
				if(AppLogic.AppConfigBool("ShowCardStartDateFields"))
					CardStartDate = ctrlCreditCardPanel.CardStartMonth.Replace(" ", "").PadLeft(2, '0') + ctrlCreditCardPanel.CardStartYear.Trim().Replace(" ", "");
                
				String CardIssueNumber = ctrlCreditCardPanel.CreditCardIssueNumber.Trim().Replace(" ", "");

                if (CardNumber.StartsWith("*"))
                {
                    // Still obscured in the form so use the original
                    CardNumber = BillingAddress.CardNumber;
                }

                if (CardExtraCode.StartsWith("*"))
                {
                    // Still obscured in the form so use the original
                    CardExtraCode = AppLogic.GetCardExtraCodeFromSession(ThisCustomer);
                }

                if (AppLogic.AppConfigBool("ValidateCreditCardNumbers"))
                {
                    BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCreditCard;
                    BillingAddress.CardName = CardName;
                    BillingAddress.CardExpirationMonth = CardExpirationMonth;
                    BillingAddress.CardExpirationYear = CardExpirationYear;
                    BillingAddress.CardStartDate = CommonLogic.IIF(CardStartDate == "00", String.Empty, CardStartDate);
                    BillingAddress.CardIssueNumber = CardIssueNumber;

                    CardType Type = CardType.Parse(strCardType);
                    CreditCardValidator validator = new CreditCardValidator(CardNumber, Type);
                    bool isValid = validator.Validate();

                    BillingAddress.CardType = strCardType;
                    if (!isValid)
                    {
                        CardNumber = string.Empty;
                        // clear the card extra code
                        AppLogic.StoreCardExtraCodeInSession(ThisCustomer, string.Empty);
                    }
                    BillingAddress.CardNumber = CardNumber;
                    BillingAddress.UpdateDB();

                    if (!isValid)
                    {
                        err = new ErrorMessage(Server.HtmlEncode(AppLogic.GetString("checkoutcard_process.aspx.3", 1, Localization.GetDefaultLocale())));
                        Response.Redirect("~/checkout1.aspx?errormsg=" + err.MessageId);
                    }
                }


                // store in appropriate session, encrypted, so it can be used when the order is actually "entered"
                AppLogic.StoreCardExtraCodeInSession(ThisCustomer, CardExtraCode);


                if (NetTotal == Decimal.Zero && AppLogic.AppConfigBool("SkipPaymentEntryOnZeroDollarCheckout"))
                {
                    // remember their info:
                    BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCreditCard;
                    BillingAddress.ClearCCInfo();
                    BillingAddress.UpdateDB();
                }
                else
                {
                    if (CardNumber.Length == 0 || (!AppLogic.AppConfigBool("CardExtraCodeIsOptional") && CardExtraCode.Length == 0) || CardName.Length == 0 || CardExpirationMonth.Length == 0 ||
                        CardExpirationYear.Length == 0)
                    {
                        pnlErrorMsg.Visible = true;
                        ErrorMsgLabel.Text = "checkoutcard_process.aspx.1".StringResource();
                        return;
                    }
                    // remember their info:
                    BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCreditCard;
                    BillingAddress.CardName = CardName;
                    BillingAddress.CardType = strCardType;
                    BillingAddress.CardNumber = CardNumber;
                    BillingAddress.CardExpirationMonth = CardExpirationMonth;
                    BillingAddress.CardExpirationYear = CardExpirationYear;
                    BillingAddress.CardStartDate = CommonLogic.IIF(CardStartDate == "00", String.Empty, CardStartDate);
                    BillingAddress.CardIssueNumber = CardIssueNumber;
                    BillingAddress.UpdateDB();
                }
            }
            else if (PM == AppLogic.ro_PMPurchaseOrder)
            {
                String PONumber = txtPO.Text.Trim();
                if (PONumber.Length == 0)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = "checkoutpayment.aspx.21".StringResource();
                    return;
                }

                // remember their info:
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMPurchaseOrder;
                BillingAddress.PONumber = PONumber;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMCODMoneyOrder)
            {
                String PONumber = CommonLogic.FormCanBeDangerousContent("PONumber");
                if (PONumber.Length == 0)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = "checkoutpayment.aspx.21".StringResource();
                    return;
                }
                // remember their info:
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCODMoneyOrder;
                BillingAddress.PONumber = PONumber;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMCODCompanyCheck)
            {
                String PONumber = CommonLogic.FormCanBeDangerousContent("PONumber");
                if (PONumber.Length == 0)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = "checkoutpayment.aspx.21".StringResource();
                    return;
                }
                // remember their info:
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCODCompanyCheck;
                BillingAddress.PONumber = PONumber;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMCODNet30)
            {
                String PONumber = CommonLogic.FormCanBeDangerousContent("PONumber");
                if (PONumber.Length == 0)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = "checkoutpayment.aspx.21".StringResource();
                    return;
                }
                // remember their info:
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCODNet30;
                BillingAddress.PONumber = PONumber;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMPayPal)
            {
                Response.Redirect("~/checkoutpayment.aspx?PaymentMethod=" + AppLogic.ro_PMPayPal + CommonLogic.IIF(RequireTerms, "&TermsAndConditionsRead=" + CommonLogic.FormCanBeDangerousContent("TermsAndConditionsRead"), ""));
            }
            else if (PM == AppLogic.ro_PMRequestQuote)
            {
                // no action required here
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMRequestQuote;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMCheckByMail)
            {
                // no action required here
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCheckByMail;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMCOD)
            {
                // no action required here
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCOD;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMECheck)
            {
                DropDownList ddlECheckBankAccountType = ctrlEcheck.FindControl("ddlECheckBankAccountType") as DropDownList;
                TextBox txtECheckBankAccountNumber = ctrlEcheck.FindControl("txtECheckBankAccountNumber") as TextBox;
                TextBox txtEcheckBankABACode = ctrlEcheck.FindControl("txtEcheckBankABACode") as TextBox;
                TextBox txtEcheckBankName = ctrlEcheck.FindControl("txtEcheckBankName") as TextBox;
                TextBox txtECheckBankAccountName = ctrlEcheck.FindControl("txtECheckBankAccountName") as TextBox;

                String ECheckBankName = txtEcheckBankName.Text;
                String ECheckBankAccountNumber = txtECheckBankAccountNumber.Text;
                String ECheckBankAccountType = ddlECheckBankAccountType.SelectedValue;
                String ECheckBankAccountName = txtECheckBankAccountName.Text;
                String ECheckBankABACode = txtEcheckBankABACode.Text;
                if (ECheckBankName.Length == 0 || ECheckBankAccountNumber.Length == 0 || ECheckBankAccountType.Length == 0 || ECheckBankAccountName.Length == 0 ||
                    ECheckBankABACode.Length == 0)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = "checkoutcard_process.aspx.1".StringResource();
                    return;
                }

                // NOTE:
                //  We should'nt do the clearing before updating the db
                //  for now let's just clear the cc details and 
                //  save the eCheck details for payment processing later
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }

                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMECheck;
                BillingAddress.ECheckBankName = ECheckBankName;
                BillingAddress.ECheckBankAccountNumber = ECheckBankAccountNumber;
                BillingAddress.ECheckBankAccountType = ECheckBankAccountType;
                BillingAddress.ECheckBankAccountName = ECheckBankAccountName;
                BillingAddress.ECheckBankABACode = ECheckBankABACode;
                
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMCardinalMyECheck)
            {
                String ACSUrl;
                String Payload;
                String TransID;
                String LookupResult;
                OrderNumber = AppLogic.GetNextOrderNumber();
                if (Cardinal.MyECheckLookup(cart, OrderNumber, NetTotal, AppLogic.AppConfig("StoreName") + " Purchase", out ACSUrl, out Payload, out TransID, out LookupResult))
                {
                    BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMCardinalMyECheck;
                    if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                    {
                        BillingAddress.ClearCCInfo();
                    }
                    BillingAddress.UpdateDB();

                    ThisCustomer.ThisCustomerSession["Cardinal.LookupResult"] = LookupResult;
                    ThisCustomer.ThisCustomerSession["Cardinal.ACSUrl"] = ACSUrl;
                    ThisCustomer.ThisCustomerSession["Cardinal.Payload"] = Payload;
                    ThisCustomer.ThisCustomerSession["Cardinal.TransactionID"] = TransID;
                    ThisCustomer.ThisCustomerSession["Cardinal.OrderNumber"] = OrderNumber.ToString();
                    if (AppLogic.ProductIsMLExpress() == false)
                    {
                        Response.Redirect("~/cardinalecheckform.aspx");
                    }
                }
                else
                {
                    // MyECheck transaction failed to start, return to checkout1 with error message
                    err = new ErrorMessage(Server.HtmlEncode("checkoutecheck.aspx.14".StringResource()));
                    Response.Redirect("~/checkout1.aspx?errormsg=" + err.MessageId);
                }
            }
            else if (PM == AppLogic.ro_PMMicropay)
            {
                BillingAddress.PaymentMethodLastUsed = AppLogic.ro_PMMicropay;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();
            }
            else if (PM == AppLogic.ro_PMPayPalExpress || PM == AppLogic.ro_PMPayPalExpressMark)
            {
                BillingAddress.PaymentMethodLastUsed = PM;
                if (!ThisCustomer.MasterShouldWeStoreCreditCardInfo)
                {
                    BillingAddress.ClearCCInfo();
                }
                BillingAddress.UpdateDB();

                Address shippingAddress = new Address();
                shippingAddress.LoadByCustomer(ThisCustomer.CustomerID, ThisCustomer.PrimaryShippingAddressID, AddressTypes.Shipping);
                String sURL = Gateway.StartExpressCheckout(cart, shippingAddress);
                Response.Redirect(sURL);
            }


            if (AppLogic.AppConfigBool("Checkout.UseOnePageCheckout.UseFinalReviewOrderPage"))
            {
                Response.Redirect("~/checkoutreview.aspx?paymentmethod=" + Server.UrlEncode(PaymentMethod));
            }


            //Execute payment processing

            // ----------------------------------------------------------------
            // Process The Order:
            // ----------------------------------------------------------------

            if (PaymentMethod.Length == 0)
            {
                pnlErrorMsg.Visible = true;
                ErrorMsgLabel.Text = "checkoutpayment.aspx.20".StringResource();
                return;
            }
            if (PM == AppLogic.ro_PMCreditCard)
            {
				if (Cardinal.EnabledForCheckout(cart.Total(true), BillingAddress.CardType))
                {
					OrderNumber = AppLogic.GetNextOrderNumber();
					
					if (Cardinal.PreChargeLookupAndStoreSession(ThisCustomer, OrderNumber, cart.Total(true), 
						BillingAddress.CardNumber, BillingAddress.CardExpirationMonth, BillingAddress.CardExpirationYear))
					{
						if (AppLogic.ProductIsMLExpress() == false)
						{
							Response.Redirect("cardinalform.aspx");// this will eventually come "back" to us in cardinal_process.aspx after going through banking system pages
						}
					}
					else
					{
						// user not enrolled or cardinal gateway returned error, so process card normally, using already created order #:

						string ECIFlag = Cardinal.GetECIFlag(BillingAddress.CardType);						

						String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, ECIFlag, String.Empty, String.Empty);
						if (status != AppLogic.ro_OK)
						{
                            pnlErrorMsg.Visible = true;
                            ErrorMsgLabel.Text = status;
                            return;
						}
						DB.ExecuteSQL("update orders set CardinalLookupResult=" + DB.SQuote(ThisCustomer.ThisCustomerSession["Cardinal.LookupResult"]) + " where OrderNumber=" + OrderNumber.ToString());
					}                
                }
                else
                {
                    // try create the order record, check for status of TX though:
                    OrderNumber = AppLogic.GetNextOrderNumber();
                    String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                    if (status == AppLogic.ro_3DSecure)
                    {
                        // If credit card is enrolled in a 3D Secure service (Verified by Visa, etc.)
                        Response.Redirect("~/secureform.aspx");
                    }
                    if (status != AppLogic.ro_OK)
                    {
                        pnlErrorMsg.Visible = true;
                        ErrorMsgLabel.Text = status;
                        return;
                    }
                }
            }
            else if (PM == AppLogic.ro_PMPurchaseOrder)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == AppLogic.ro_PMCODMoneyOrder)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == "CODCOMPANYCHEC")
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == AppLogic.ro_PMCODNet30)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == AppLogic.ro_PMPayPal)
            {
            }
            else if (PM == AppLogic.ro_PMPayPalExpress || PM == AppLogic.ro_PMPayPalExpressMark)
            {
                // will never make it this far due to redirect to PayPal.
            }
            else if (PM == AppLogic.ro_PMRequestQuote)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == AppLogic.ro_PMCheckByMail)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == AppLogic.ro_PMCOD)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == AppLogic.ro_PMECheck)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }
            else if (PM == AppLogic.ro_PMMicropay)
            {
                // try create the order record, check for status of TX though:
                OrderNumber = AppLogic.GetNextOrderNumber();
                String status = Gateway.MakeOrder(String.Empty, AppLogic.TransactionMode(), cart, OrderNumber, String.Empty, String.Empty, String.Empty, String.Empty);
                if (status != AppLogic.ro_OK)
                {
                    pnlErrorMsg.Visible = true;
                    ErrorMsgLabel.Text = status;
                    return;
                }
            }

            Response.Redirect("~/orderconfirmation.aspx?ordernumber=" + OrderNumber.ToString() + "&paymentmethod=" + Server.UrlEncode(PaymentMethod));
        }

        private string WriteECHECKPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");
           
            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMECheck + "\">\n");
            s.Append(BillingAddress.InputECheckHTML(true));
            s.Append("<br/>");
            s.Append("<p align=\"center\">");
            
            s.Append("</p>");
          
            return s.ToString();
        }

        private string WritePURCHASEORDERPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMPurchaseOrder + "\">\n");
            s.Append("<b>" + "checkoutpo.aspx.3".StringResource() + "</b><br/><br/>");
            s.Append("checkoutpo.aspx.4".StringResource());
            s.Append("<input type=\"text\" name=\"PONumber\" size=\"20\" maxlength=\"50\">\n");
            s.Append("<input type=\"hidden\" name=\"PONumber_vldt\" value=\"[req][blankalert=" + "checkoutpo.aspx.5".StringResource() + "]\">");
            s.Append("<br/>");
            s.Append("<br/>");
            s.Append("<p align=\"center\">");
          
            s.Append("</p>");
        
            return s.ToString();
        }

        private string WriteCODMONEYORDERPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMCODMoneyOrder + "\">\n");
            s.Append("<b>" + "checkoutpo.aspx.3".StringResource() + "</b><br/><br/>");
            s.Append("checkoutpo.aspx.4".StringResource());
            s.Append("<input type=\"text\" name=\"PONumber\" size=\"20\" maxlength=\"50\">\n");
            s.Append("<input type=\"hidden\" name=\"PONumber_vldt\" value=\"[req][blankalert=" + "checkoutpo.aspx.5".StringResource() + "]\">");
            s.Append("<br/>");
            s.Append("<p align=\"center\">");
           
            s.Append("</p>");
       
            return s.ToString();
        }

        private string WriteCODCOMPANYCHECKPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMCODCompanyCheck + "\">\n");
            s.Append("<b>" + "checkoutpo.aspx.3".StringResource() + "</b><br/><br/>");
            s.Append("checkoutpo.aspx.4".StringResource());
            s.Append("<input type=\"text\" name=\"PONumber\" size=\"20\" maxlength=\"50\">\n");
            s.Append("<input type=\"hidden\" name=\"PONumber_vldt\" value=\"[req][blankalert=" + "checkoutpo.aspx.5".StringResource() + "]\">");
            s.Append("<br/>");
            s.Append("<p align=\"center\">");
            
            s.Append("</p>");
      
            return s.ToString();
        }

        private string WriteCODNET30Pane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMCODNet30 + "\">\n");
            s.Append("<b>" + "checkoutpo.aspx.3".StringResource() + "</b><br/><br/>");
            s.Append("checkoutpo.aspx.4".StringResource());
            s.Append("<input type=\"text\" name=\"PONumber\" size=\"20\" maxlength=\"50\">\n");
            s.Append("<input type=\"hidden\" name=\"PONumber_vldt\" value=\"[req][blankalert=" + "checkoutpo.aspx.5".StringResource()+ "]\">");
            s.Append("<br/>");
            s.Append("<p align=\"center\">");
          
            s.Append("</p>");
       
            return s.ToString();
        }

        // note, this payment method cannot support finalization instructions!
        private string WritePayPalPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMPayPal + "\">\n");
            return s.ToString();
        }

        private string WriteREQUESTQUOTEPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

           
            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMRequestQuote + "\">\n");
            s.Append("<p align=\"center\">");
            
            s.Append("</p>");
       
            return s.ToString();
        }

        private string WriteCardinalMyECheckPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");
            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMCardinalMyECheck + "\">\n");
            s.Append("<p align=\"center\">");
            s.Append("</p>");
            return s.ToString();
        }

        private string WriteCHECKBYMAILPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMCheckByMail + "\">\n");
            s.Append("<p align=\"center\">");
            
            s.Append("</p>");
         
            return s.ToString();
        }

        private string WriteCODPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            
            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMCOD + "\">\n");
            s.Append("<p align=\"center\">");
           
            s.Append("</p>");
        
            return s.ToString();
        }

        private string WriteMICROPAYPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            if (ThisCustomer.MicroPayBalance >= NetTotal)
            {
               
                s.Append(OrderFinalizationInstructions);
                s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMMicropay + "\">\n");
                s.Append("<p align=\"center\">");
               
                s.Append("</p>");
            
            }
            else
            {
                s.Append(String.Format("checkoutpayment.aspx.26".StringResource(), ThisCustomer.CurrencyString(ThisCustomer.MicroPayBalance)));
            }

            return s.ToString();
        }

        private string WritePAYPALEXPRESSPane(String OrderFinalizationInstructions, Address BillingAddress, bool RequireTerms, string PM)
        {
            StringBuilder s = new StringBuilder("");

            s.Append("<p align=\"center\">" + "checkoutpaypal.aspx.2".StringResource() + "</p><br/>");
            s.Append(OrderFinalizationInstructions);
            s.Append("<input type=\"hidden\" name=\"paymentmethod\" value=\"" + AppLogic.ro_PMPayPalExpressMark + "\">\n");
            s.Append("<p align=\"center\">");
            s.Append("</p>");

            return s.ToString();
        }

        private void ResetPaymentPanes()
        {
            SelectedPaymentType = ViewState["SelectedPaymentType"].ToString();
            SetPasswordFields();
            String ShippingMethodIDFormField = CommonLogic.FormCanBeDangerousContent("ShippingMethodID").Replace(",", ""); // remember to remove the hidden field which adds a comma to the form post (javascript again)
            int ShippingMethodID = 0;
            String ShippingMethod = String.Empty;
            if (cart.ShipCalcID !=
                Shipping.ShippingCalculationEnum.UseRealTimeRates)
            {
                ShippingMethodID = Localization.ParseUSInt(ShippingMethodIDFormField);
                ShippingMethod = Shipping.GetShippingMethodName(ShippingMethodID, null);
            }
            else
            {
                if (ShippingMethodIDFormField.Length != 0 &&
                    ShippingMethodIDFormField.IndexOf('|') != -1)
                {
                    String[] frmsplit = ShippingMethodIDFormField.Split('|');
                    ShippingMethodID = Localization.ParseUSInt(frmsplit[0]);
                    ShippingMethod = String.Format("{0}|{1}", frmsplit[1], frmsplit[2]);
                }
            }
            String sql = String.Format("update ShoppingCart set ShippingMethodID={0}, ShippingMethod={1} where CustomerID={2} and CartType={3}", ShippingMethodID.ToString(), DB.SQuote(ShippingMethod), ThisCustomer.CustomerID.ToString(), ((int) CartTypeEnum.ShoppingCart).ToString());
            DB.ExecuteSQL(sql);
            cart = new ShoppingCart(SkinID, ThisCustomer, CartTypeEnum.ShoppingCart, 0, false);
            InitializeShippingOptions(ref cart);
            OrderSummary.Text = cart.DisplaySummary(true, true, true, true, false);

            ctrlPaymentMethod.ShowCREDITCARD = true;//pnlCreditCardPane.Visible = false;
            pnlPOPane.Visible = false;
            ctrlPaymentMethod.ShowCODMONEYORDER = false;
            ctrlPaymentMethod.ShowCODCOMPANYCHECK = false;
            ctrlPaymentMethod.ShowCODNET30 = false;
            ctrlPaymentMethod.ShowPAYPAL = false;
            ctrlPaymentMethod.ShowREQUESTQUOTE = false;
            ctrlPaymentMethod.ShowCHECKBYMAIL = false;
            ctrlPaymentMethod.ShowCOD = false;
            pnlEcheckPane.Visible = false;
            ctrlPaymentMethod.ShowMICROPAY = false;
            ctrlPaymentMethod.ShowPAYPALEXPRESS = false;

            btnCheckOut.Text = "checkout1.aspx.1".StringResource();
        }

        #endregion



        #region ASHLAND

        protected void ctrlBillingAddress_SelectedCountryChanged(object sender, EventArgs e)
        {
            AddressControl ctrlAddress = sender as AddressControl;

            if (ctrlAddress != null)
            {
                StateDropDownData(ctrlAddress);
            }
        }

        protected void ctrlShippingAddress_SelectedCountryChanged(object sender, EventArgs e)
        {
            AddressControl ctrlAddress = sender as AddressControl;

            if (ctrlAddress != null)
            {
                StateDropDownData(ctrlAddress);
            }
        }
        
        private void PopulateAddressControlValues(AddressControl addrControl, AddressTypes addressType)
        {
            addrControl.AllowEdit = !ThisCustomer.IsRegistered;
            divBillingCopy.Visible = !ThisCustomer.IsRegistered;
            divBillingHeader.Visible = !ThisCustomer.IsRegistered;

            Address customerAddress = null;

            if (addressType == AddressTypes.Billing)
            {
                customerAddress = BillingAddress;
            }

            if (addressType == AddressTypes.Shipping)
            {
                customerAddress = ShippingAddress;
            }

            if (addrControl != null)
            {

                addrControl.NickName = customerAddress.NickName;
                addrControl.FirstName = customerAddress.FirstName;
                addrControl.LastName = customerAddress.LastName;
                addrControl.PhoneNumber = customerAddress.Phone;
                addrControl.Company = customerAddress.Company;
                addrControl.ResidenceType= customerAddress.ResidenceType.ToString();
                addrControl.Address1 = customerAddress.Address1;
                addrControl.Address2 = customerAddress.Address2;
                addrControl.Suite = customerAddress.Suite;
                addrControl.City = customerAddress.City;
                addrControl.ZipCode = customerAddress.Zip;
                CountryDropDownData(!ThisCustomer.IsRegistered, addrControl, customerAddress);
                addrControl.Country = customerAddress.Country;
                StateDropDownData(addrControl);
                addrControl.State = customerAddress.State;
            }
        }

        private void CountryDropDownData(bool editMode, AddressControl ctrlAddress, Address customerAddress)
        {
            //Assign Datasource for the country dropdown
            if (editMode)
            {
                ctrlAddress.CountryDataSource = Country.GetAll();
            }
            else
            {
                List<Country> tmplst = new List<Country>();
                tmplst.Add(new Country { ID = customerAddress.CountryID, Name = customerAddress.Country });

                ctrlAddress.CountryDataSource = tmplst;
            }
            ctrlAddress.CountryDataTextField = "Name";
            ctrlAddress.CountryDataValueField = "Name";
        }

        private void StateDropDownData(AddressControl ctrlAddress)
        {
            //Assign Datasource for the state dropdown
            ctrlAddress.StateDataSource = State.GetAllStateForCountry(AppLogic.GetCountryID(ctrlAddress.Country), ThisCustomer.LocaleSetting);
        }

        protected void ctrlPaymentMethod_OnPaymentMethodChanged(object sender, EventArgs e)
        {
            WritePaymentPanels();
            pnlErrorMsg.Visible = false;
            SetCreditCardVisibility(ctrlPaymentMethod.CREDITCARDChecked);
            pnlEcheckPane.Visible = ctrlPaymentMethod.ECHECKChecked;
            pnlPOPane.Visible = ctrlPaymentMethod.PURCHASEORDERChecked || ctrlPaymentMethod.CODMONEYORDERChecked || ctrlPaymentMethod.CODCOMPANYCHECKChecked || ctrlPaymentMethod.CODNET30Checked;
            InitializePageContentPayment();
        }

        protected void SetCreditCardVisibility(Boolean IsVisible)
        {
            if (!IsVisible)
            {
                pnlCCPane.Visible = CCPaneInfo.Visible = false;
                return;
            }
            pnlCCPane.Visible = true;
            pnlCCPaneInfo.Visible = false;

            if (GWActual != null && !string.IsNullOrEmpty(GWActual.CreditCardPaneInfo(SkinID, ThisCustomer)))
            {
                CCPaneInfo.Text = GWActual.CreditCardPaneInfo(SkinID, ThisCustomer);
                pnlCCPane.Visible = false;
                pnlCCPaneInfo.Visible = true;
            }
        }


        private void WritePaymentPanels()
        {            
            // When PAYPALPRO is active Gateway or PAYPALEXPRESS is available Payment Method
            // then we want to make the PayPal Express Mark available
            if ((AppLogic.ActivePaymentGatewayCleaned() == Gateway.ro_GWPAYPALPRO || AllowedPaymentMethods.IndexOf(AppLogic.ro_PMPayPalExpress) > -1)
                && AllowedPaymentMethods.IndexOf(AppLogic.ro_PMPayPalExpressMark) == -1)
            {
                if (AllowedPaymentMethods.Length != 0)
                {
                    AllowedPaymentMethods += ",";
                }
                AllowedPaymentMethods += AppLogic.ro_PMPayPalExpressMark;
            }

            Address BillingAddress = new Address();
            BillingAddress.LoadByCustomer(ThisCustomer.CustomerID, ThisCustomer.PrimaryBillingAddressID, AddressTypes.Billing);
            bool EChecksAllowed = GWActual != null && GWActual.SupportsEChecks(); // let manual gw use echecks so site testing can occur
            bool POAllowed = AppLogic.CustomerLevelAllowsPO(ThisCustomer.CustomerLevelID);
            bool CODCompanyCheckAllowed = ThisCustomer.CODCompanyCheckAllowed;
            bool CODNet30Allowed = ThisCustomer.CODNet30Allowed;

            StringBuilder OrderFinalizationInstructions = new StringBuilder(4096);
            String OrderFinalizationXmlPackageName = AppLogic.AppConfig("XmlPackage.OrderFinalization");
            String OrderFinalizationXmlPackageFN = Server.MapPath("xmlpackages/" + OrderFinalizationXmlPackageName);

            if (CommonLogic.FileExists(OrderFinalizationXmlPackageFN))
            {
                OrderFinalizationInstructions.Append("<p align=\"left\"><b>" + "checkoutreview.aspx.24".StringResource()+ "</b></p>");
                OrderFinalizationInstructions.Append(AppLogic.RunXmlPackage(OrderFinalizationXmlPackageName, null, ThisCustomer, SkinID, string.Empty, string.Empty, false, false));
            }
            if (OrderFinalizationInstructions.Length != 0)
            {
                OrderFinalizationInstructions.Append("<br/>");
            }
            Finalization.Text = OrderFinalizationInstructions.ToString(); // set the no payment panel here, in case it is needed        

            foreach (String PM in AllowedPaymentMethods.Split(','))
            {
                String PMCleaned = AppLogic.CleanPaymentMethod(PM);
                if (PMCleaned == AppLogic.ro_PMCreditCard)
                {
                    pnlFinalization.Visible = Finalization.Text.Length != 0;
                    PMFinalization.Text = OrderFinalizationInstructions.ToString();

                    if (!IsPostBack)
                    {
                        ctrlPaymentMethod.CREDITCARDChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCreditCard);
                        SetCreditCardVisibility((BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCreditCard));

                    }
                    
                    if (ctrlCreditCardPanel.CreditCardName == "")
                    {
                        ctrlCreditCardPanel.CreditCardName = BillingAddress.CardName;
                    }
                    if (ctrlCreditCardPanel.CreditCardNumber == "")
                    {
                        ctrlCreditCardPanel.CreditCardNumber = AppLogic.SafeDisplayCardNumber(BillingAddress.CardNumber, "Address", BillingAddress.AddressID);
                    }
                    if (ctrlCreditCardPanel.CreditCardVerCd == "")
                    {
                        ctrlCreditCardPanel.CreditCardVerCd = AppLogic.SafeDisplayCardExtraCode(AppLogic.GetCardExtraCodeFromSession(ThisCustomer));
                    }
                    if (ctrlCreditCardPanel.CreditCardType == "address.cs.32".StringResource())
                    {
                        ctrlCreditCardPanel.CreditCardType = BillingAddress.CardType;
                    }
                    if (ctrlCreditCardPanel.CardExpMonth == "address.cs.34".StringResource())
                    {
                        ctrlCreditCardPanel.CardExpMonth = BillingAddress.CardExpirationMonth;
                    }
                    if (ctrlCreditCardPanel.CardExpYr == "address.cs.35".StringResource())
                    {
                        ctrlCreditCardPanel.CardExpYr = BillingAddress.CardExpirationYear;
                    }
                    if (!CommonLogic.IsStringNullOrEmpty(BillingAddress.CardStartDate))
                    {
                        if (ctrlCreditCardPanel.CardStartMonth == "address.cs.34".StringResource())
                        {
                            ctrlCreditCardPanel.CardStartMonth = BillingAddress.CardStartDate.Substring(0, 2);
                        }
                        if (ctrlCreditCardPanel.CardStartYear == "address.cs.35".StringResource())
                        {
                            ctrlCreditCardPanel.CardStartYear = BillingAddress.CardStartDate.Substring(2, 4);
                        }
                    }
                    if (ctrlCreditCardPanel.CreditCardIssueNumber == "")
                    {
                        ctrlCreditCardPanel.CreditCardIssueNumber = BillingAddress.CardIssueNumber;
                    }
                    
                    
                    ctrlPaymentMethod.ShowCREDITCARD = true;

                }
                else if (PMCleaned == AppLogic.ro_PMPurchaseOrder)
                {
                    if (POAllowed)
                    {
                        ctrlPaymentMethod.ShowPURCHASEORDER = true;
                        PMFinalization.Text = OrderFinalizationInstructions.ToString();
                        pnlFinalization.Visible = Finalization.Text.Length != 0;

                        if (!IsPostBack)
                        {
                            ctrlPaymentMethod.PURCHASEORDERChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMPurchaseOrder);
                            pnlPOPane.Visible = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMPurchaseOrder);                            
                        }

                        if (txtPO.Text == "")
                        {
                            txtPO.Text = BillingAddress.PONumber;
                        }
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMCODMoneyOrder)
                {
                    if (POAllowed)
                    {
                        ctrlPaymentMethod.ShowCODMONEYORDER = true;
                        PMFinalization.Text = OrderFinalizationInstructions.ToString();
                        pnlFinalization.Visible = Finalization.Text.Length != 0;

                        if (!IsPostBack)
                        {
                            ctrlPaymentMethod.CODMONEYORDERChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCODMoneyOrder);
                            pnlPOPane.Visible = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCODMoneyOrder);
                        }

                        if (txtPO.Text == "")
                        {
                            txtPO.Text = BillingAddress.PONumber;
                        }
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMCODCompanyCheck)
                {
                    if (CODCompanyCheckAllowed)
                    {
                        ctrlPaymentMethod.ShowCODCOMPANYCHECK = true;
                        PMFinalization.Text = OrderFinalizationInstructions.ToString();
                        pnlFinalization.Visible = Finalization.Text.Length != 0;

                        if (!IsPostBack)
                        {
                            ctrlPaymentMethod.CODCOMPANYCHECKChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCODCompanyCheck);
                            pnlPOPane.Visible = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCODCompanyCheck);
                        }

                        if (txtPO.Text == "")
                        {
                            txtPO.Text = BillingAddress.PONumber;
                        }
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMCODNet30)
                {
                    if (CODNet30Allowed)
                    {
                        ctrlPaymentMethod.ShowCODNET30 = true;
                        PMFinalization.Text = OrderFinalizationInstructions.ToString();
                        pnlFinalization.Visible = Finalization.Text.Length != 0;

                        if (!IsPostBack)
                        {
                            ctrlPaymentMethod.PAYPALChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCODNet30);
                            pnlPOPane.Visible = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCODNet30);
                        }

                        if (txtPO.Text == "")
                        {
                            txtPO.Text = BillingAddress.PONumber;
                        }
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMPayPal)
                {
                    ctrlPaymentMethod.ShowPAYPAL = true;

                    if (!IsPostBack)
                    {
                        ctrlPaymentMethod.PAYPALChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMPayPal);
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMPayPalExpressMark)
                {
                    ctrlPaymentMethod.ShowPAYPALEXPRESS = true;
                    PMFinalization.Text = OrderFinalizationInstructions.ToString();
                    pnlFinalization.Visible = Finalization.Text.Length != 0;

                    if (!IsPostBack)
                    {
                        ctrlPaymentMethod.PAYPALEXPRESSChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMPayPalExpress);
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMRequestQuote)
                {
                    ctrlPaymentMethod.ShowREQUESTQUOTE = true;
                    PMFinalization.Text = OrderFinalizationInstructions.ToString();
                    pnlFinalization.Visible = Finalization.Text.Length != 0;

                    if (!IsPostBack)
                    {
                        ctrlPaymentMethod.REQUESTQUOTEChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMRequestQuote);
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMCheckByMail)
                {
                    ctrlPaymentMethod.ShowCHECKBYMAIL = true;
                    PMFinalization.Text = OrderFinalizationInstructions.ToString();
                    pnlFinalization.Visible = Finalization.Text.Length != 0;

                    if (!IsPostBack)
                    {
                        ctrlPaymentMethod.CHECKBYMAILChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCheckByMail);
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMCardinalMyECheck)
                {
                    ctrlPaymentMethod.ShowCARDINALMYECHECK = true;
                    PMFinalization.Text = OrderFinalizationInstructions.ToString();
                    pnlFinalization.Visible = Finalization.Text.Length != 0;

                    if (!IsPostBack)
                    {
                        ctrlPaymentMethod.CARDINALMYECHECKChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCardinalMyECheck);
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMCOD)
                {
                    ctrlPaymentMethod.ShowCOD = true;
                    PMFinalization.Text = OrderFinalizationInstructions.ToString();
                    pnlFinalization.Visible = Finalization.Text.Length != 0;

                    if (!IsPostBack)
                    {
                        ctrlPaymentMethod.CODChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMCOD);
                    }
                }
                else if (PMCleaned == AppLogic.ro_PMECheck)
                {
                    if (EChecksAllowed)
                    {
                        pnlFinalization.Visible = Finalization.Text.Length != 0;
                        ctrlEcheck.ECheckBankABAImage1 = AppLogic.LocateImageURL("~/App_Themes/skin_" + SkinID.ToString() + "/images/check_aba.gif");                                                         
                        ctrlEcheck.ECheckBankABAImage2 = AppLogic.LocateImageURL("~/App_Themes/skin_" + SkinID.ToString() + "/images/check_aba.gif");
                        ctrlEcheck.ECheckBankAccountImage = AppLogic.LocateImageURL("~/App_Themes/skin_" + SkinID.ToString() + "/images/check_account.gif");
                        ctrlEcheck.ECheckNoteLabel = string.Format("address.cs.48".StringResource(), AppLogic.LocateImageURL("~/App_Themes/skin_" + SkinID.ToString() + "/images/check_micr.gif"));

                        if (!IsPostBack)
                        {
                            ctrlPaymentMethod.ECHECKChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMECheck);
                            pnlEcheckPane.Visible = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMECheck);

                            if (ctrlEcheck.ECheckBankAccountName == "")
                            {
                                ctrlEcheck.ECheckBankAccountName = BillingAddress.ECheckBankAccountName;
                            }
                            if (ctrlEcheck.ECheckBankName == "")
                            {
                                ctrlEcheck.ECheckBankName = BillingAddress.ECheckBankName;
                            }
                            if (ctrlEcheck.ECheckBankABACode == "")
                            {
                                ctrlEcheck.ECheckBankABACode = AppLogic.SafeDisplayCardNumber(BillingAddress.ECheckBankABACode, "Address", BillingAddress.AddressID);
                            }
                            if (ctrlEcheck.ECheckBankAccountNumber == "")
                            {
                                ctrlEcheck.ECheckBankAccountNumber = AppLogic.SafeDisplayCardNumber(BillingAddress.ECheckBankAccountNumber, "Address", BillingAddress.AddressID);
                            }
                        }

                        ctrlPaymentMethod.ShowECHECK = true;

                    }
                }
                else if (PMCleaned == AppLogic.ro_PMMicropay)
                {
                    if (AppLogic.MicropayIsEnabled())
                    {
                        ctrlPaymentMethod.ShowMICROPAY = true;

                        if (ctrlPaymentMethod.MICROPAYChecked)
                        {
                            PMFinalization.Text = OrderFinalizationInstructions.ToString();
                            pnlFinalization.Visible = (ThisCustomer.MicroPayBalance >= NetTotal && PMFinalization.Text.Length != 0);
                            btnCheckOut.Visible = ThisCustomer.MicroPayBalance >= NetTotal;
                            ctrlPaymentMethod.ShowMICROPAYMessage = ThisCustomer.MicroPayBalance < NetTotal;
                            ctrlPaymentMethod.MICROPAYLabel = String.Format("checkoutpayment.aspx.26".StringResource(), ThisCustomer.CurrencyString(ThisCustomer.MicroPayBalance));
                        }
                        else
                        {
                            btnCheckOut.Visible = true;
                            ctrlPaymentMethod.ShowMICROPAYMessage = false;
                        }

                        if (!IsPostBack)
                        {
                            ctrlPaymentMethod.MICROPAYChecked = (BillingAddress.PaymentMethodLastUsed == AppLogic.ro_PMMicropay);
                        }
                    }
                }
            }

            if (!IsPostBack && BillingAddress.PaymentMethodLastUsed == String.Empty && AppLogic.AppConfig("PaymentMethods").Contains("checkoutpayment.aspx.7".StringResource()))
            {
                ctrlPaymentMethod.CREDITCARDChecked = true;
                SetCreditCardVisibility(true);
            }

            Boolean GWRequiresFinalization = GWActual != null && GWActual.RequiresFinalization();

            if (
                ctrlPaymentMethod.PAYPALChecked || 
                (ThisCustomer.MicroPayBalance < NetTotal && ctrlPaymentMethod.MICROPAYChecked) ||
                (ctrlPaymentMethod.CREDITCARDChecked && GWRequiresFinalization)
            )
            {
                pnlFinalization.Visible = false;
            }

            if (!IsPostBack)
            {
                SetDefaultCheckedPaymentMethod(OrderFinalizationInstructions.ToString());
            }

        }

        public void SetDefaultCheckedPaymentMethod(string orderFinalizationInstructions)
        {
            if (!ctrlPaymentMethod.HasPaymentMethodSelected)
            {
                for (int i = 0; i < AllowedPaymentMethods.Split(',').Length; i++)
                {
                    if (ctrlPaymentMethod.ShowCREDITCARD)
                    {
                        ctrlPaymentMethod.CREDITCARDChecked = true;
                        SetCreditCardVisibility(true);
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowPURCHASEORDER)
                    {
                        ctrlPaymentMethod.PURCHASEORDERChecked = true;
                        pnlPOPane.Visible = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowCODMONEYORDER)
                    {
                        ctrlPaymentMethod.CODMONEYORDERChecked = true;
                        pnlPOPane.Visible = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowCODCOMPANYCHECK)
                    {
                        ctrlPaymentMethod.CODCOMPANYCHECKChecked = true;
                        pnlPOPane.Visible = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowCODNET30)
                    {
                        ctrlPaymentMethod.PAYPALChecked = true;
                        pnlPOPane.Visible = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowPAYPAL)
                    {
                        ctrlPaymentMethod.PAYPALChecked = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowPAYPALEXPRESS)
                    {
                        ctrlPaymentMethod.PAYPALEXPRESSChecked = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowREQUESTQUOTE)
                    {
                        ctrlPaymentMethod.REQUESTQUOTEChecked = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowCHECKBYMAIL)
                    {
                        ctrlPaymentMethod.CHECKBYMAILChecked = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowCARDINALMYECHECK)
                    {
                        ctrlPaymentMethod.CARDINALMYECHECKChecked = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowCOD)
                    {
                        ctrlPaymentMethod.CODChecked = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowECHECK)
                    {
                        ctrlPaymentMethod.ECHECKChecked = true;
                        pnlEcheckPane.Visible = true;
                        break;
                    }
                    else if (ctrlPaymentMethod.ShowMICROPAY)
                    {
                        PMFinalization.Text = orderFinalizationInstructions;
                        pnlFinalization.Visible = (ThisCustomer.MicroPayBalance >= NetTotal && PMFinalization.Text.Length != 0);
                        btnCheckOut.Visible = ThisCustomer.MicroPayBalance >= NetTotal;
                        ctrlPaymentMethod.ShowMICROPAYMessage = ThisCustomer.MicroPayBalance < NetTotal;
                        ctrlPaymentMethod.MICROPAYLabel = String.Format(AppLogic.GetString("checkoutpayment.aspx.26", SkinID, ThisCustomer.LocaleSetting), ThisCustomer.CurrencyString(ThisCustomer.MicroPayBalance));
                    }
                }
            }
        }




        #endregion

        protected void btnCheckOut_Click(object sender, EventArgs e)
        {
            ProcessCheckout();
        }
}
}
