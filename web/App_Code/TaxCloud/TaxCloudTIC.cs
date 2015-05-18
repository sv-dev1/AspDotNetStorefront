using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using System.Text;
using System.Data.SqlClient;
using AspDotNetStorefrontCore;
using System.Data;

/// <summary>
///LovelyEcom Added For Taxcloud TICs
/// </summary>
  [Serializable]
public class TaxCloudTIC
{
    public string TicID { get; set; }
    public int Ssuta { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string DisplayOrder { get; set; }
	public TaxCloudTIC()
	{
		//
		//TODO: 在此处添加构造函数逻辑
		//

	}
   
}
  public class TaxCloudTICs
  {
      public IList<TaxCloudTIC> tics
      {
          get;
          set;
      }

      public  TaxCloudTICs()
      {
          tics=new List<TaxCloudTIC>();
      }
      public void LoadFeed()
      {
          XElement node = XElement.Load(@"https://taxcloud.net/tic/xml/");
          ParseElement(node);
      }

      public void SaveToDB()
      {
          //StringBuilder strQury = new StringBuilder();
          //strQury.Append("Delete TaxClass where TaxCode=@TaxCode INSERT INTO [dbo].[TaxClass]([TaxClassGUID],[Name],[TaxCode],[DisplayOrder],[CreatedOn],[Ssuta],[Description])");
          //strQury.Append("VALUES (@TaxClassGUID,@Name,@TaxCode,@DisplayOrder,@CreatedOn,@Ssuta,@Description)");
          int index = 2;
          foreach (var item in tics)
          {
              SqlParameter[] pars = { DB.CreateSQLParameter("@TaxClassGUID", SqlDbType.UniqueIdentifier,128,Guid.NewGuid(),ParameterDirection.Input),
                                        DB.CreateSQLParameter("@Name",SqlDbType.NVarChar,400,item.Title,ParameterDirection.Input),
                                        DB.CreateSQLParameter("@TaxCode",SqlDbType.NVarChar,100,item.TicID,ParameterDirection.Input),
                                        DB.CreateSQLParameter("@DisplayOrder",SqlDbType.Int,4,index,ParameterDirection.Input),
                                        DB.CreateSQLParameter("@CreatedOn",SqlDbType.DateTime,128,DateTime.Now, ParameterDirection.Input),
                                        DB.CreateSQLParameter("@Ssuta",SqlDbType.TinyInt,4,item.Ssuta,ParameterDirection.Input),
                                        DB.CreateSQLParameter("@Description", SqlDbType.NVarChar,500,item.Description, ParameterDirection.Input),
                                    
                                      };
              DB.ExecuteStoredProcInt("Sync_TaxcloudTaxCode", pars);
              index++;
          }
      }

      public void ClearDB()
      {
     
      }

      private void ParseElement(XElement xElement)
      {
          var elements = xElement.Elements("tic");
          if (elements.Count() > 0)
          {
              foreach (var item in elements)
              {
                  TaxCloudTIC tax = new TaxCloudTIC();
                  tax.Description = item.Element("description").Value;
                  tax.Ssuta = (item.Attribute("ssuta").Value == "true") ? 1 : 0;
                  tax.TicID = item.Attribute("id").Value;
                  tax.Title = item.Attribute("title").Value;
               
                  tics.Add(tax);
                  var el = item.Elements("tic");
                  if (el != null)
                      ParseElement(item);
              }
          }
      }
      
  }