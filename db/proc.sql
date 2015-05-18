create proc Sync_TaxcloudTaxCode
@TaxClassGUID uniqueidentifier,
@Name nvarchar(400),
@TaxCode nvarchar(100),
@DisplayOrder int,
@CreatedOn datetime,
@Ssuta TinyInt,
@Description nvarchar(500)

as

declare @isHasCode int

select @isHasCode=COUNT(1)  from TaxClass where TaxCode=@TaxCode
if @isHasCode>0 begin
	update [TaxClass] set [TaxClassGUID]=@TaxClassGUID, Name=@Name+'-'+@TaxCode, DisplayOrder =@DisplayOrder,CreatedOn =@CreatedOn,[Ssuta]=@Ssuta, [Description]=@Description
	where [TaxCode]=@TaxCode
	end
else 
begin
INSERT INTO [dbo].[TaxClass]([TaxClassGUID],[Name],[TaxCode],[DisplayOrder],[CreatedOn],[Ssuta],[Description])
VALUES (@TaxClassGUID,@Name+'-'+@TaxCode,@TaxCode,@DisplayOrder,@CreatedOn,@Ssuta,@Description)
end