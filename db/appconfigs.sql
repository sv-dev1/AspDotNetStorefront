

IF NOT EXISTS (SELECT * FROM [AppConfig] WHERE [Name] = 'taxcloud.Enabled') BEGIN
INSERT [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ConfigValue,ValueType) values(0,'taxcloud.Enabled','TaxCloud','This setting turns the TaxCloud integration on (or off)','true','boolean')
END


IF NOT EXISTS (SELECT * FROM [AppConfig] WHERE [Name] = 'taxcloud.apiloginid') BEGIN
INSERT [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ConfigValue) values(0,'taxcloud.apiloginid','TaxCloud','Your TaxCloud API ID is available in the Websites area of your TaxCloud account.','32CE9780')
END


IF NOT EXISTS (SELECT * FROM [AppConfig] WHERE [Name] = 'taxcloud.apikey') BEGIN
INSERT [dbo].AppConfig(SuperOnly,Name,GroupName,Description,ConfigValue) values(0,'taxcloud.apikey','TaxCloud','Your TaxCloud API Key is available in the Websites area of your TaxCloud account.','2A6DE752-22A4-4D98-A43C-28D67A10E8AF')
END



IF NOT EXISTS (SELECT * FROM [AppConfig] WHERE [Name] = 'taxcloud.ShippingTaxClassCode') BEGIN
INSERT into AppConfig(Name,Description,ConfigValue,ValueType)
values('taxcloud.ShippingTaxClassCode','This is the TaxCloud TIC for your shipping - see https://taxcloud.net/tic/','11010','integer')
END


IF NOT EXISTS (SELECT * FROM [AppConfig] WHERE [Name] = 'taxcloud.deliveredBySeller') BEGIN
INSERT into AppConfig(Name,Description,ConfigValue,ValueType)
values('taxcloud.deliveredBySeller','Is the purchase delivered by a seller vehicle (not a 3rd parter shipper).','false','boolean')
END

 
update AppConfig set ConfigValue='BasicOPC' where Name='Checkout.Type'

update AppConfig set ConfigValue='false' where Name='Checkout.UseOnePageCheckout.UseFinalReviewOrderPage'


update AppConfig set ConfigValue='taxcloud' where Name='VerifyAddressesProvider'
--VerifyAddressesProvider.USPS.UserID
