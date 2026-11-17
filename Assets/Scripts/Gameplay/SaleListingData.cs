using System;
using System.Collections.Generic;

[Serializable]
public sealed class SaleListingData
{
    public bool initialized;
    public int askingPrice;
    public int fairValue;
    public List<HaggleSession> buyers = new List<HaggleSession>();
}
