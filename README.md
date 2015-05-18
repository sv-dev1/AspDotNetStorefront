#TaxCloud AddIn for AspDotNetStorefront

Welcome to TaxCloud's AddIn for AspDotNetStorefront.

This AddIn was initially developed for AspDotNetStorefront version 9.3.1.1 and updated to support version 9.4. Source code of this AddIn and associated documentation files are being made publicly available pursuant to the MIT License.

This GitHub repository is managed by The Federal Tax Authority, LLC ("FedTax"), the proud ceator and operator of [TaxCloud](https://taxcloud.net).

 ##What is TaxCloud##
TaxCloud is an online service designed to handle every aspect of sales tax management, from collection to filing.

###Sales Tax Calculation###	
* Calculates sales tax in real time for **every state, county, city, and special jurisdiction in the United States**
* Keeps track of which types of products are exempt from sales tax in which states
* Monitors changes to tax rates and tax holidays and updates data accordingly

For more information about our service, please visit [TaxCloud.net](https://taxcloud.net).

 ##About this AspDotNetStorefront Module/Plug-in##

 AspDotNetStorefront supports [manual configuration of tax rates](http://manual.aspdotnetstorefront.com/p-972-taxes.aspx), however, ongoing configuration and maintenece of individual sales tax jurisdictions is tedious at best. Even if you could keep all the possible sales tax rates current, you still would have to deal with taxability (product-specific exemptions) and sales tax hol;idays. To eliminate these burdens and automate sales tax compliance efforts, AspDotNetStorefront store owners and opertors can now connect their storefront installation with TaxCloud using this sample AddIn.

 #Installing TaxCloud AddIn for AspDotNetStorefront#

1. Register for your free TaxCloud account at [https://taxcloud.net/](https://taxcloud.net/account/register/).
2. Login and configure your TaxCloud account to control where you collect sales tax.
3. Get your TaxCloud **API ID** and **API KEY** from the [TaxCloud "Websites" Area](https://taxcloud.net/account/websites/).
4. **BACKUP YOUR CURRENT ASPDNSF INSTALLATION DIRECTORIES, INCLUDING WWWROOT AND ADMIN BEFORE PROCEEDING.**
5. Copy source files from [this GitHub Repository](https://github.com/taxcloud/AspDotNetStorefront) into the respective locations in your ASPDNSF installation.
6. Run/Apply the SQL scripts in this GitHub Repository's /db/ directory.
6. In the ASPDNSF Administration Console, set the folling AppConfig elements with information for your new TaxCloud account:
  * **taxcloud.apiloginid** - This is your TaxCloud API ID mentioned above.
  * **taxcloud.apikey** - This is your TaxCloud API KEY mentioned above.
  * **taxcloud.ShippingTaxClassCode** - This is the shipping Taxability Information Code, usually '11010' if you are using Real-Time-Shipping and you **do not markup shipping**. You should use '11000' if you charge flat-rate shipping, or markup your shipping cost such that you are charging your customers more than your actual shipping cost.
  * **taxcloud.Enabled** - This setting turns the TaxCloud AddIn on (if 'true') or off (if 'false').
  * **RTShipping.OriginAddress** - Street address of where you are shipping orders from. *If you are using Real-Time-Shipping, you should already have this set*.
  * **RTShipping.OriginAddress2** -  Second street address of where you are shipping orders from. *If you are using Real-Time-Shipping, you should already have this set*.
  * **RTShipping.OriginCity** - The City where you are shipping orders from. *If you are using Real-Time-Shipping, you should already have this set*.
  * **RTShipping.OriginState** - The **Two Character Abbreviation** of the State where you are shipping orders from. *If you are using Real-Time-Shipping, you should already have this set*.
  * **RTShipping.OriginZip** - The 5-digit zip code of where you are shipping orders from. *If you are using Real-Time-Shipping, you should already have this set*.
  * **VerifyAddressesProvider.USPS.UserID** - If you alread have a USPS WebTools User ID, then leave this unchanged. If you do not have a USPS WebTools User ID and you will be using TaxCloud for Address Verification (the next setting), then you can use the fake USPS ID '111CLOUD1111' (because TaxCloud does not rely upon your USPS ID to validate addresses)
  * **VerifyAddressesProvider** - Set to 'TaxCloud' if using TaxCloud for address verification.
7. Click the **Reset Cache** link in the ASPDNSF Administration Console.
8. **You have now successfully installed the Taxcloud AddIn**
9. Test your installation by going through your normal storefront checkout process.
10. **IMPORTANT:** Once you have complete a test order, including marking the order as shipped/completed, you **must return to TaxCloud to set your website as LIVE**. Failure to complete this final step will prevent TaxCloud from preparing any sales tax reports.


