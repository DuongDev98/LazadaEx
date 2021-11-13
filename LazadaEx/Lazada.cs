using Lazop.Api;
using Lazop.Api.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazadaEx
{
    public class LazadaController
    {
        private string accessToken = "";
        public string appKey;
        public string appSecret;
        public string urlRedirect;

        public LazadaController(string appKey, string appSecret, string urlRedirect)
        {
            this.appKey = appKey;
            this.appSecret = appSecret;
            this.urlRedirect = urlRedirect;
        }

        public LazadaController(string appKey, string appSecret, string urlRedirect, string accessToken)
        {
            this.appKey = appKey;
            this.appSecret = appSecret;
            this.urlRedirect = urlRedirect;
            this.accessToken = accessToken;
        }

        public List<OrderItem> GetOrderItems(List<string> lstOrderIds, ref string error)
        {
            int LIMIT_SIZE = 100;
            string temp = "";
            List<string> lstId = new List<string>();
            for (int i = 0; i < lstOrderIds.Count; i++)
            {
                temp += "," + lstOrderIds[i];
                if (i + 1 % LIMIT_SIZE == 0 || i == lstOrderIds.Count - 1)
                {
                    temp = temp.Substring(1);
                    lstId.Add("[" + temp + "]");
                    temp = "";
                }
            }
            List<OrderItem> lst = new List<OrderItem>();
            //Lấy danh sách chi tiết
            foreach (string id in lstId)
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                dic.Add("order_ids", id);
                LazopResponse rs = LazadaRequest("/orders/items/get", accessToken, dic, true);
                if (rs.IsError())
                {
                    error += "<br>" + rs.Message;
                }
                else
                {
                    JArray arr = JArray.Parse(Convert.ToString(JObject.Parse(rs.Body)["data"]));
                    //JArray arr = JArray.Parse(Convert.ToString(JObject.Parse(dataJsonDetails)["data"]));
                    foreach (JObject orderItem in arr)
                    {
                        JArray orderArr = JArray.Parse(Convert.ToString(orderItem["order_items"]));
                        foreach (JObject item in orderArr)
                        {
                            OrderItem it = JsonConvert.DeserializeObject<OrderItem>(item.ToString());
                            lst.Add(it);
                        }
                    }
                }
            }
            return lst;
        }

        public List<Order> GetOrder(DateTime createAfter, ref string errorStr)
        {
            List<Order> lstOrder = new List<Order>();
            bool error = false;
            int offset = 0, limit = 1, count = 0;
            while (!error && (offset < count || (offset == 0 && count == 0)))
            {
                Dictionary<string, string> dic = new Dictionary<string, string>();
                //request.AddApiParameter("update_before", "2018-02-10T16:00:00+08:00");
                //request.AddApiParameter("created_before", "2018-02-10T16:00:00+08:00");
                //request.AddApiParameter("status", "shipped");
                dic.Add("offset", Convert.ToString(offset));
                dic.Add("limit", Convert.ToString(limit));
                dic.Add("sort_by", "created_at");
                dic.Add("sort_direction", "DESC");
                dic.Add("created_after", createAfter.ToString("o"));
                //dic.Add("update_before", updateAfter.ToString("o"));
                LazopResponse response = LazadaRequest("/orders/get", accessToken, dic, true);
                if (response.IsError())
                {
                    JObject data = JObject.Parse(response.Body);
                    errorStr = data["message"].ToString();
                    error = true;
                }
                else
                {
                    JObject dataJson = JObject.Parse(response.Body);
                    //JObject dataJson = JObject.Parse(dataJsonOrder);
                    JObject data = JObject.Parse(dataJson["data"].ToString());
                    if (count == 0) count = Convert.ToInt32(dataJson["countTotal"]);
                    JArray orders = JArray.Parse(data["orders"].ToString());
                    foreach (JObject item in orders)
                    {
                        Order itemNew = JsonConvert.DeserializeObject<Order>(item.ToString());
                        lstOrder.Add(itemNew);
                    }
                }
                offset += limit;
            }
            return lstOrder;
        }

        public LzResult GetCategoryProperties(string categoryId)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("primary_category_id", categoryId);
            LazopResponse response = LazadaRequest("/category/attributes/get", accessToken, dic, true);
            if (response.IsError())
            {
                return new LzResult("", Convert.ToString(JObject.Parse(response.Body)["message"]));
            }
            else
            {
                return new LzResult(Convert.ToString(JObject.Parse(response.Body)["data"]));
            }
        }

        public LzResult GetCategoryTree()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("language_code", "vi_VN");
            LazopResponse response = LazadaRequest("/category/tree/get", "", dic, true);
            if (response.IsError())
            {
                return new LzResult("", Convert.ToString(JObject.Parse(response.Body)["message"]));
            }
            else
            {
                return new LzResult(Convert.ToString(JObject.Parse(response.Body)["data"]));
            }
        }

        public LzResult DeleteProduct(string datajson)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("seller_sku_list", datajson);
            LazopResponse response = LazadaRequest("/product/remove", accessToken, dic);
            if (response.IsError())
            {
                return new LzResult("", GetError(JObject.Parse(response.Body)));
            }
            else
            {
                return new LzResult();
            }
        }

        public LzResult UpdateProduct(string payload)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("payload", payload);
            LazopResponse response = LazadaRequest("/product/update", accessToken, dic);
            if (response.IsError())
            {
                return new LzResult("", GetError(JObject.Parse(response.Body)));
            }
            else
            {
                return new LzResult();
            }
        }

        public LzResult InsertProduct(string payload)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("payload", payload);
            LazopResponse response = LazadaRequest("/product/create", accessToken, dic);
            if (response.IsError())
            {
                return new LzResult("", GetError(JObject.Parse(response.Body)));
            }
            else
            {
                JObject data = JObject.Parse(Convert.ToString(JObject.Parse(response.Body)["data"]));
                return new LzResult(Convert.ToString(data["item_id"]));
            }
        }

        public LzResult GetAllSkuIdByProductId(string id)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("item_id", id);
            LazopResponse response = LazadaRequest("/product/item/get", accessToken, dic, true);
            JObject item = JObject.Parse(response.Body);
            if (response.IsError())
            {
                return new LzResult("", Convert.ToString(item["message"]), Convert.ToString(item["code"]));
            }
            else
            {
                string jsonArr = "";
                JArray arrSkus = JArray.Parse(Convert.ToString(JObject.Parse(Convert.ToString(item["data"]))["skus"]));
                foreach (JObject it in arrSkus)
                {
                    jsonArr += ",\"" + Convert.ToString(it["SkuId"]) + "\"";
                }
                if (jsonArr.Length > 0) jsonArr = jsonArr.Substring(1);
                jsonArr = "[" + jsonArr + "]";
                return new LzResult(jsonArr);
            }
        }

        public LzResult GetAccessToken(string code)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("code", code);
            LazopResponse res = LazadaRequest("/auth/token/create", "", dic);
            JObject item = JObject.Parse(res.Body);
            if (res.IsError()) return new LzResult("", Convert.ToString(item["message"]), Convert.ToString(item["code"]));
            else return new LzResult(Convert.ToString(item["access_token"]), Convert.ToInt32(item["expires_in"]));
        }

        LazopResponse LazadaRequest(string path, string accessToken = "", Dictionary<string, string> param = null, bool usingGetMethod = false)
        {
            ILazopClient client = new LazopClient(UrlConstants.API_GATEWAY_URL_VN, appKey, appSecret);
            if (path == "/auth/token/create")
            {
                client = new LazopClient(UrlConstants.API_AUTHORIZATION_URL, appKey, appSecret);
            }
            LazopRequest request = new LazopRequest();
            request.SetApiName(path);
            if (param != null && param.Count > 0)
            {
                foreach (string key in param.Keys)
                {
                    request.AddApiParameter(key, param[key]);
                }
            }

            if (usingGetMethod) request.SetHttpMethod("GET");

            LazopResponse response;
            if (accessToken == string.Empty) response = client.Execute(request);
            else response = client.Execute(request, accessToken);
            return response;
        }

        string GetError(JObject data)
        {
            string message = data["message"].ToString();
            string errorDetail = "";
            JToken detail = data["detail"];
            if (detail != null)
            {
                JArray arr = JArray.Parse(detail.ToString());
                foreach (JObject error in arr)
                {
                    if (errorDetail.Length > 0) errorDetail += Environment.NewLine;
                    errorDetail += error["field"] + ":" + error["message"];
                }
            }
            if (errorDetail.Length > 0)
            {
                message += Environment.NewLine + errorDetail;
            }
            return message;
        }

        //Lớp hỗ trợ api Lazada
        public class Order
        {
            public string voucher_platform { set; get; }
            public string voucher { set; get; }
            public string warehouse_code { set; get; }
            public string order_number { set; get; }
            public string voucher_seller { set; get; }
            public string created_at { set; get; }
            public string voucher_code { set; get; }
            public string gift_option { set; get; }
            public string shipping_fee_discount_platform { set; get; }
            public string customer_last_name { set; get; }
            public string promised_shipping_times { set; get; }
            public string updated_at { set; get; }
            public string price { set; get; }
            public string national_registration_number { set; get; }
            public string shipping_fee_original { set; get; }
            public string payment_method { set; get; }
            public string address_updated_at { set; get; }
            public string customer_first_name { set; get; }
            public string shipping_fee_discount_seller { set; get; }
            public string shipping_fee { set; get; }
            public string branch_number { set; get; }
            public string tax_code { set; get; }
            public string items_count { set; get; }
            public string delivery_info { set; get; }
            public List<string> statuses { set; get; }
            public Address address_billing { set; get; }
            public string extra_attributes { set; get; }
            public string order_id { set; get; }
            public string remarks { set; get; }
            public string gift_message { set; get; }
            public Address address_shipping { set; get; }
        }

        public class OrderItem
        {
            public string tax_amount { set; get; }
            public string reason { set; get; }
            public string sla_time_stamp { set; get; }
            public string voucher_seller { set; get; }
            public string purchase_order_id { set; get; }
            public string voucher_code_seller { set; get; }
            public string voucher_code { set; get; }
            public string package_id { set; get; }
            public string buyer_id { set; get; }
            public string variation { set; get; }
            public string voucher_code_platform { set; get; }
            public string purchase_order_number { set; get; }
            public string sku { set; get; }
            public string order_type { set; get; }
            public string invoice_number { set; get; }
            public string cancel_return_initiator { set; get; }
            public string shop_sku { set; get; }
            public string is_reroute { set; get; }
            public string stage_pay_status { set; get; }
            public string tracking_code_pre { set; get; }
            public string order_item_id { set; get; }
            public string shop_id { set; get; }
            public string order_flag { set; get; }
            public string is_fbl { set; get; }
            public string name { set; get; }
            public string delivery_option_sof { set; get; }
            public string order_id { set; get; }
            public string status { set; get; }
            public string product_main_image { set; get; }
            public string paid_price { set; get; }
            public string product_detail_url { set; get; }
            public string warehouse_code { set; get; }
            public string promised_shipping_time { set; get; }
            public string shipping_type { set; get; }
            public string created_at { set; get; }
            public string voucher_seller_lpi { set; get; }
            public string shipping_fee_discount_platform { set; get; }
            public string wallet_credits { set; get; }
            public string updated_at { set; get; }
            public string currency { set; get; }
            public string shipping_provider_type { set; get; }
            public string voucher_platform_lpi { set; get; }
            public string shipping_fee_original { set; get; }
            public string item_price { set; get; }
            public string shipping_service_cost { set; get; }
            public string tracking_code { set; get; }
            public string shipping_fee_discount_seller { set; get; }
            public string shipping_amount { set; get; }
            public string reason_detail { set; get; }
            public string return_status { set; get; }
            public string shipment_provider { set; get; }
            public string voucher_amount { set; get; }
            public string digital_delivery_info { set; get; }
            public string extra_attributes { set; get; }
        }

        public class Address
        {
            public string country { set; get; }
            public string address1 { set; get; }
            public string address2 { set; get; }
            public string address3 { set; get; }
            public string address4 { set; get; }
            public string address5 { set; get; }
            public string phone { set; get; }
            public string phone2 { set; get; }
            public string city { set; get; }
            public string post_code { set; get; }
            public string first_name { set; get; }
            public string last_name { set; get; }
        }

        public class LzResult
        {
            public string data { set; get; }

            public string message { set; get; }

            public int expires_in { set; get; }

            public bool success { get { return message == null || message.Length == 0; } }

            public string code { set; get; }

            public LzResult(string data, string message, int expires_in)
            {
                this.data = data;
                this.message = message;
                this.expires_in = expires_in;
            }

            public LzResult(string data, string message, string code)
            {
                this.data = data;
                this.message = message;
                this.code = code;
            }

            public LzResult()
            {
            }

            public LzResult(string data)
            {
                this.data = data;
            }

            public LzResult(string data, string message)
            {
                this.data = data;
                this.message = message;
            }

            public LzResult(string data, int expires_in)
            {
                this.data = data;
                this.expires_in = expires_in;
            }
        }
    }
}
