namespace LoadDataFtp
{
    public class RecDetail
    {
        public long IdXml { get; set; }
        public long DocumentId { get; set; }
        public long GoodId { get; set; }
        public string GoodsItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MoneySum { get; set; }
        public decimal BonusSum { get; set; }
    }
}