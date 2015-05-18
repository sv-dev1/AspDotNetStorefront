<%@ Page language="c#" Inherits="AspDotNetStorefront.checkoutpayment" CodeFile="checkoutpayment.aspx.cs" MasterPageFile="~/App_Templates/Skin_1/template.master" %>
<%@ Register TagPrefix="aspdnsfc" Namespace="AspDotNetStorefrontControls" Assembly="AspDotNetStorefrontControls" %>
<%@ Register TagPrefix="aspdnsf" TagName="Topic" Src="~/Controls/TopicControl.ascx" %>
<%@ Register TagPrefix="aspdnsf" TagName="XmlPackage" Src="~/Controls/XmlPackageControl.ascx" %>
<%@ Register TagPrefix="aspdnsf" TagName="OrderOption" Src="~/controls/OrderOption.ascx" %>
<%@ Register TagPrefix="aspdnsf" TagName="BuySafeKicker" Src="~/controls/BuySafeKicker.ascx" %>
<%@ Register Src="CIM/WalletSelector.ascx" TagName="WalletSelector" TagPrefix="uc1" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="PageContent">
    <asp:Panel ID="pnlContent" runat="server" >
        <asp:Literal ID="JSPopupRoutines" runat="server"></asp:Literal>

        <asp:Panel ID="pnlHeaderGraphic" runat="server" HorizontalAlign="center">
            <asp:ImageMap ID="checkoutheadergraphic" HotSpotMode="PostBack" runat="server">
                <asp:RectangleHotSpot AlternateText="" HotSpotMode="Navigate" NavigateUrl="~/shoppingcart.aspx" Top="0" Left="0" Bottom="54" Right="87" />
                <asp:RectangleHotSpot AlternateText="" HotSpotMode="Navigate" NavigateUrl="~/account.aspx?checkout=true" Top="0" Left="87" Bottom="54" Right="173" />
                <asp:RectangleHotSpot AlternateText="" HotSpotMode="Inactive" NavigateUrl="~/checkoutshipping.aspx" Top="0" Left="173" Bottom="54" Right="259" />
            </asp:ImageMap>
        </asp:Panel>
        
        <asp:Panel ID="pnlErrorMsg" runat="server" Visible="false">
            <asp:Label ID="ErrorMsgLabel" CssClass="error" runat="server"></asp:Label>
        <br/>
    </asp:Panel>
    <asp:Label ID="lblNetaxeptErrorMsg" style="font-weight: bold; color: Red;" Visible="false" runat="server"></asp:Label>
    <asp:ValidationSummary DisplayMode="List" ID="valsumCreditCard" ShowMessageBox="false" runat="server" ShowSummary="true" ValidationGroup="creditcard" ForeColor="red" Font-Bold="true"/>
    <asp:ValidationSummary DisplayMode="List" ID="valsumEcheck" ShowMessageBox="false" runat="server" ShowSummary="true" ValidationGroup="echeck" ForeColor="red" Font-Bold="true"/>
    <asp:Panel ID="pnlCCTypeErrorMsg" runat="server" Visible="false">
        <asp:Label ID="CCTypeErrorMsgLabel" style="font-weight: bold; color: Red;" runat="server" Text="<%$ Tokens:StringResource, address.cs.19 %>">
            <br/>
        </asp:Label>
    </asp:Panel>
    <asp:Panel ID="pnlCCExpDtErrorMsg" runat="server" Visible="false">
        <asp:Label ID="CCExpDtErrorMsg" style="font-weight: bold; color: Red;" runat="server" Text="<%$ Tokens:StringResource, checkoutcard_process.aspx.2 %>">
        <br />
        </asp:Label>
    </asp:Panel>
    <asp:Label ID="Label1" style="font-weight: bold; color: Red;" runat="server"></asp:Label>
    <br />
        <aspdnsf:Topic runat="server" ID="CheckoutPaymentPageHeader" TopicName="CheckoutPaymentPageHeader" />
        <asp:Literal ID="XmlPackage_CheckoutPaymentPageHeader" runat="server" Mode="PassThrough"></asp:Literal>
        
            <asp:Panel ID="pnlNoPaymentRequired" runat="server" HorizontalAlign="Center" Visible="false">
                <asp:Label ID="NoPaymentRequired" runat="server" CssClass="InfoMessage" /><br /><br />
                <asp:Literal ID="Finalization" runat="server" Mode="PassThrough"></asp:Literal>
                <asp:Button ID="btnContinueCheckOut1" runat="server" Text="<%$ Tokens:StringResource,checkoutpayment.aspx.18 %>" CssClass="PaymentPageContinueCheckoutButton" />
            </asp:Panel>

            <asp:Panel ID="pnlPaymentOptions" runat="server" HorizontalAlign="left" Visible="true">
                <table style="width:100%;">
                    <tr>
                        <td>
                            <aspdnsfc:PaymentMethod ID="ctrlPaymentMethod" runat="server" 
                                OnPaymentMethodChanged="ctrlPaymentMethod_OnPaymentMethodChanged"   
                                CARDINALMYECHECKCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.13 %>" 
                                CHECKBYMAILCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.11 %>" 
                                CODCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.12 %>" 
                                CODCOMPANYCHECKCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.22 %>" 
                                CODMONEYORDERCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.24 %>" 
                                CODNET30Caption="<%$ Tokens:StringResource, checkoutpayment.aspx.23 %>" 
                                CREDITCARDCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.7 %>" 
                                ECHECKCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.13 %>" 
                                MICROPAYCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.14 %>" 
                                MICROPAYLabel="<%$ Tokens:StringResource, checkoutpayment.aspx.26 %>" 
                                AMAZONSIMPLEPAYCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.31 %>"              
                                PAYPALCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.9 %>" 
                                PAYPALEXPRESSCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.25 %>" 
                                PAYPALEXPRESSLabel="<%$ Tokens:StringResource, checkoutpaypal.aspx.2 %>" 
                                PAYPALLabel="<%$ Tokens:StringResource, checkoutpaypal.aspx.2 %>"
                                PURCHASEORDERCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.8 %>"
                                REQUESTQUOTECaption="<%$ Tokens:StringResource, checkoutpayment.aspx.10 %>"
								MONEYBOOKERSQUICKCHECKOUTCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.32 %>"
								MONEYBOOKERSQUICKCHECKOUTLabel="<%$ Tokens:StringResource, checkoutpayment.aspx.33 %>"
                                SECURENETVAULTCaption="<%$ Tokens:StringResource, checkoutpayment.aspx.36 %>"
                                PAYPALHOSTEDCHECKOUTCaption='<%$ Tokens:StringResource, checkoutpayment.aspx.37 %>'
                                CREDITCARDImage_AmericanExpress="~/App_Themes/Skin_1/images/cc_americanexpress.jpg"
                                CREDITCARDImage_Discover="~/App_Themes/Skin_1/images/cc_discover.jpg"
                                CREDITCARDImage_MasterCard="~/App_Themes/Skin_1/images/cc_mastercard.jpg"
                                CREDITCARDImage_Visa="~/App_Themes/Skin_1/images/cc_visa.jpg"
                                CREDITCARDImage_Laser="~/App_Themes/Skin_1/images/cc_laser.gif"
                                CREDITCARDImage_Maestro="~/App_Themes/Skin_1/images/cc_maestro.jpg"
                                CREDITCARDImage_VisaDebit="~/App_Themes/Skin_1/images/cc_visadebit.gif"
                                CREDITCARDImage_VisaElectron="~/App_Themes/Skin_1/images/cc_visaelectron.png"
                                CREDITCARDImage_Jcb="~/App_Themes/Skin_1/images/cc_jcb.gif"
                                CREDITCARDImage_Diners="~/App_Themes/Skin_1/images/cc_diners.gif"
                                PAYPALEXPRESSImage="<%$ Tokens:AppConfig, PayPal.PaymentIcon %>"
                                PAYPALEMBEDDEDCHECKOUTImage="<%$ Tokens:AppConfig, PayPal.PaymentIcon %>"
                                PAYPALImage="<%$ Tokens:AppConfig, PayPal.PaymentIcon %>" 
								MONEYBOOKERSQUICKCHECKOUTImage="<%$ Tokens:AppConfig, Moneybookers.QuickCheckout.PaymentIcon %>" />
                        </td>
                        <td style="width:210px;">
                            <div style="position:relative;">
                                <div style="position:absolute;top:-27px;">
                                    <aspdnsf:BuySafeKicker ID="buySAFEKicker" WrapperClass="paymentKicker" runat="server" />
                                </div>
                            </div>
                        </td>
                    </tr>
                </table>
            
            <br />            
            <asp:Panel ID="pnlFinalization" runat="server" Visible="false" CssClass ="InfoMessageBox">
                <asp:Literal ID="PMFinalization" Mode="PassThrough" runat="server"></asp:Literal>
            </asp:Panel>

			<%-- CIM --%>
			<asp:Panel ID="PanelWallet" runat="server" Visible="false">
			    <asp:ScriptManagerProxy ID="SMCIM" runat="server"></asp:ScriptManagerProxy>
			    <uc1:WalletSelector ID="CimWalletSelector" runat="server" />
			</asp:Panel>
			<br />
			<%-- CIM End --%>

             <asp:Panel ID="pnlCCPane" runat="server" Visible="false" CssClass ="InfoMessageBox">
                <aspdnsfc:CreditCardPanel ID="ctrlCreditCardPanel" runat="server" 
                     CreditCardExpDtCaption="<%$ Tokens:StringResource, address.cs.33 %>" 
                     CreditCardNameCaption="<%$ Tokens:StringResource, address.cs.23 %>" 
                     CreditCardNoSpacesCaption="<%$ Tokens:StringResource, shoppingcart.cs.106 %>"
                     CreditCardNumberCaption="<%$ Tokens:StringResource, address.cs.25 %>" 
                     CreditCardTypeCaption="<%$ Tokens:StringResource, address.cs.31 %>"
                     CreditCardVerCdCaption="<%$ Tokens:StringResource, address.cs.28 %>" 
                     HeaderCaption="<%$ Tokens:StringResource, checkoutcard.aspx.6 %>" 
                     WhatIsThis="<%$ Tokens:StringResource, address.cs.50 %>" 
                     CCNameReqFieldErrorMessage="<%$ Tokens:StringResource, address.cs.24 %>" 
                     CreditCardStartDtCaption="<%$ Tokens:StringResource, address.cs.59 %>"
                     CreditCardIssueNumCaption="<%$ Tokens:StringResource, address.cs.61 %>"
                     CreditCardIssueNumNote="<%$ Tokens:StringResource, address.cs.63 %>"
                     CCNameValGrp="creditcard" CCNumberReqFieldErrorMessage="<%$ Tokens:StringResource, address.cs.26 %>" 
                     CCNumberValGrp="creditcard" CCVerCdReqFieldErrorMessage="<%$ Tokens:StringResource, address.cs.29 %>" 
                     CCVerCdValGrp="creditcard" ShowCCVerCd="True" ShowCCStartDtFields="<%$ Tokens:AppConfigBool, ShowCardStartDateFields %>"
                     ShowCCVerCdReqVal="<%$ Tokens:AppConfigBool, CardExtraCodeIsOptional %>"
					 CimSaveCardCaption="<%$ Tokens:StringResource, address.cs.72 %>" />
             </asp:Panel>            
            <asp:Panel ID="pnlEcheckPane" runat="server" Visible="false" CssClass ="InfoMessageBox">
                <aspdnsfc:Echeck ID="ctrlEcheck" runat="server" 
                    ECheckBankABACodeLabel1="<%$ Tokens:StringResource, address.cs.41 %>" 
                    ECheckBankABACodeLabel2="<%$ Tokens:StringResource, address.cs.42 %>" 
                    ECheckBankAccountNameLabel="<%$ Tokens:StringResource, address.cs.36 %>" 
                    ECheckBankAccountNumberLabel1="<%$ Tokens:StringResource, address.cs.44 %>" 
                    ECheckBankAccountNumberLabel2="<%$ Tokens:StringResource, address.cs.45 %>" 
                    ECheckBankAccountTypeLabel="<%$ Tokens:StringResource, address.cs.47 %>" 
                    ECheckBankNameLabel1="<%$ Tokens:StringResource, address.cs.38 %>" 
                    ECheckBankNameLabel2="<%$ Tokens:StringResource, address.cs.40 %>" 
                    ECheckNoteLabel="<%$ Tokens:StringResource, address.cs.48 %>"
                    BankAccountNameReqFieldErrorMessage="<%$ Tokens:StringResource,address.cs.37 %>"
                    BankNameReqFieldErrorMessage="<%$ Tokens:StringResource, address.cs.39 %>"
                    BankABACodeReqFieldErrorMessage="<%$ Tokens:StringResource, address.cs.43 %>"
                    BankAccountNumberReqFieldErrorMessage="<%$ Tokens:StringResource, address.cs.46 %>"
                    BankAccountNameReqFieldValGrp="echeck" BankNameReqFieldValGrp="echeck"
                    BankABACodeReqFieldValGrp="echeck" BankAccountNumberReqFieldValGrp="echeck" />
            </asp:Panel>
           <asp:Panel ID="pnlPOPane" runat="server" Visible="false" CssClass="InfoMessageBox">
                <div><table><tr><td><b>
                    <asp:Label ID="lblPOHeader" runat="server" 
                        Text="<%$ Tokens:StringResource, checkoutpo.aspx.3 %>"></asp:Label>
                <br/><br/></b></td></tr><tr><td>
                    <asp:Label ID="lblPO" runat="server" 
                            Text="<%$ Tokens:StringResource, checkoutpo.aspx.4 %>"></asp:Label>
                    <asp:TextBox ID="txtPO" runat="server"></asp:TextBox>
                </td></tr></table></div>
            </asp:Panel>

            <asp:Panel ID="pnlSecureNetVaultPayment" runat="server" CssClass="InfoMessageBox" Visible="false">
                <asp:Label ID="lblSecureNetMessage" Visible="false" runat="server" CssClass="error" />
                <asp:RadioButtonList ID="rblSecureNetVaultMethods" runat="server" />
            </asp:Panel>
            <asp:Panel ID="pnlPayPalEmbeddedCheckout" runat="server" CssClass="InfoMessageBox" Visible="false">
                <asp:Literal ID="litPayPalEmbeddedCheckoutFrame" runat="server" />
            </asp:Panel>
            
            <asp:Panel ID="pnlCardinaleCheckTopic" runat="server" CssClass ="InfoMessageBox" >
                <aspdnsf:Topic runat="server" ID="CardinaleCheckTopic" TopicName="CardinalMyECheckPageHeader" />
            </asp:Panel>
            
            <asp:Panel ID="pnlCCPaneInfo" runat="server" CssClass ="InfoMessageBox InfoMessage">
                <asp:Literal ID="CCPaneInfo" Mode="PassThrough" runat="server"></asp:Literal>
            </asp:Panel>
            
			<asp:Panel ID="pnlExternalPaymentMethod" runat="server" CssClass="InfoMessageBox" Visible="false">
				<iframe runat="server" id="ExternalPaymentMethodFrame" class="ExternalPaymentMethodFrame" />
			</asp:Panel>

            <asp:Panel ID="pnlRequireTerms" runat="server" Visible="<%$ Tokens:AppConfigBool, RequireTermsAndConditionsAtCheckout %>" style="width: 99%; border: 0px; border-style: solid; padding-left: 0px; padding-top: 0px; padding-right: 0px; padding-bottom: 0px;">
                <asp:Literal ID="RequireTermsandConditions" runat="server" ></asp:Literal><br />
            </asp:Panel>
            
            <asp:Panel ID="pnlTerms" runat="server" Visible="false">
                <asp:Literal ID="terms" Mode="PassThrough" runat="server"></asp:Literal>
            </asp:Panel>
            <asp:Panel ID="pnlAmazonContCheckout" runat="server" Visible="false" HorizontalAlign="Center">
                <asp:ImageButton ID="ibAmazonSimplePay" runat="server" OnClick="btnAmazonCheckout_Click" />
            </asp:Panel>    

			
			<%= new GatewayCheckoutByAmazon.CheckoutByAmazon().RenderWalletWidget("CBAAddressWidgetContainer", false)%>
			    
            <asp:Panel ID="pnlContCheckout" runat="server" Visible="true" HorizontalAlign="Center">
                    <br/><asp:Button ID="btnContCheckout" runat="server" class="PaymentPageContinueCheckoutButton"
                        onclick="btnContCheckout_Click" 
                        Text="<%$ Tokens:StringResource, checkoutpayment.aspx.18 %>" />
            </asp:Panel>            
        </asp:Panel>
        <asp:Panel ID="pnlOrderSummary" runat="server">
             <%--lovelyEcom Taxcloud--%>
                <div>
                  <script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.4.4/jquery.min.js"></script>
    <script type="text/javascript">
        var certLink = 'xmptlink';
        var ajaxLoad = true;
        var reloadWithSave = true;
        var certSelectUrl = 'lovelyEcomCertSel.aspx';
        var merchantNameForCert = "My Store, LLC";
        //NOTE these could/should all be the same URL, but not necessary.
        var saveCertUrl = 'lovelyEcomCertAdd.aspx'; //Save a new exemption cert
        var certListUrl = 'lovelyEcomCertlist.aspx'; //list existing exemption certs for customer http://taxcloud.net/imgs/cert/sample_cert_list.aspx
        var certRemoveUrl = 'lovelyEcomCertDel.aspx'; //Note: this will be called asynchronosly (no page refresh);
        //reload the cart/checkout page after selecting a certificate (will force a new sales tax lookup with the exemption cert applied so rate will return zero)
        //if set to false, the script will not ask the customer to reload.
        var withConfirm = true;
        //use this to pass the certificate id to the server for any reason
        var hiddenCertificateField = "taxcloud_exemption_certificate";
        //Please do not edit the following line.
        var clearUrl = "?time=" + new Date().getTime().toString(); // prevent caching
        (function () {
            var tcJsHost = (("https:" == document.location.protocol) ? "https:" : "http:"); var ts = document.createElement('script'); ts.type = 'text/javascript'; ts.async = true;
            ts.src = tcJsHost + '//taxcloud.net/imgs/cert.min.js' + clearUrl; var t = document.getElementsByTagName('script')[0]; t.parentNode.insertBefore(ts, t);
        })();
    </script>
	  <span id="xmptlink" class="navlink">Are you exempt?</span>
      <input type="hidden" id="taxcloud_exemption_certificate">
      <div id="tcCertResult"></div>
                </div>
              <%--  lovely End--%>  
            
            <%--Shopping cart control--%>
        <aspdnsfc:ShoppingCartControl ID="ctrlShoppingCart"
            ProductHeaderText='<%$ Tokens:StringResource, shoppingcart.product %>'
            QuantityHeaderText='<%$ Tokens:StringResource, shoppingcart.quantity %>'
            SubTotalHeaderText='<%$ Tokens:StringResource, shoppingcart.subtotal %>' 

            runat="server" AllowEdit="false"> 
             <LineItemSettings 
                LinkToProductPageInCart='<%$ Tokens:AppConfigBool, LinkToProductPageInCart %>' 
                SKUCaption='<%$ Tokens:StringResource, showproduct.aspx.21 %>' 
                GiftRegistryCaption='<%$ Tokens:StringResource, shoppingcart.cs.92 %>'
                ItemNotesCaption='<%$ Tokens:StringResource, shoppingcart.cs.86 %>'
                ItemNotesColumns='<%$ Tokens:AppConfigUSInt, ShoppingCartItemNotesTextareaCols %>'
                ItemNotesRows='<%$ Tokens:AppConfigUSInt, ShoppingCartItemNotesTextareaRows %>'
                AllowShoppingCartItemNotes="false"
              />
        </aspdnsfc:ShoppingCartControl>
        
        <asp:Literal ID="OrderSummary" Mode="PassThrough" runat="server"></asp:Literal>
        
        <br />
        <aspdnsf:OrderOption id="ctrlOrderOption" runat="server" EditMode="false" />
        
        <%--Total Summary--%>
        <aspdnsfc:CartSummary ID="ctrlCartSummary" runat="server"             
            SubTotalCaption='<%$Tokens:StringResource, shoppingcart.cs.96 %>'
            SubTotalWithDiscountCaption='<%$Tokens:StringResource, shoppingcart.cs.97 %>'
            ShippingCaption='<%$Tokens:StringResource, shoppingcart.aspx.12 %>'
            ShippingVatExCaption='<%$Tokens:StringResource, setvatsetting.aspx.7 %>'
            ShippingVatInCaption='<%$Tokens:StringResource, setvatsetting.aspx.6 %>'
            TaxCaption='<%$Tokens:StringResource, shoppingcart.aspx.14 %>'
            TotalCaption='<%$Tokens:StringResource, shoppingcart.cs.61 %>'
            GiftCardTotalCaption='<%$Tokens:StringResource, order.cs.83 %>'            
            LineItemDiscountCaption ="<%$Tokens:StringResource, shoppingcart.cs.200 %>" OrderDiscountCaption="<%$Tokens:StringResource, shoppingcart.cs.201 %>"
            ShowGiftCardTotal="true"
            IncludeTaxInSubtotal="false"
            />
        </asp:Panel>        
            <aspdnsf:Topic runat="server" ID="CheckoutPaymentPageFooter" TopicName="CheckoutPaymentPageFooter" />
            <asp:Literal ID="XmlPackage_CheckoutPaymentPageFooter" runat="server" Mode="PassThrough"></asp:Literal>
    
    </asp:Panel>
</asp:Content>





    
