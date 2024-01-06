using NUnit.Framework;

using Data = LongYearlyValueDataCollection.Data;

public class YearlyDataTests 
{
    private static readonly short FIRST_YEAR = 1987;
    private static readonly short NEW_YEAR = 2023;
    private static readonly short OTHER_YEAR = 2020;
    private static readonly long DATA_A = 9870965360;
    private static readonly long DATA_B = 204856409423;
    private static readonly bool LOG_FAIL = false;

    [Test] public void YearlyValueDataCollectionShouldStartEmpty()
    {
        var yearly = new LongYearlyValueDataCollection();
        Assert.AreEqual( yearly.DataCount, 0, $"New Yearly Value Data should start empty but contains {yearly.DataCount} elements" );
    }
    
    [Test] public void EmptyYearlyValueDataCollectionShouldReturnNullForGetMostRecent()
    {
        var yearly = new LongYearlyValueDataCollection();
        var data = new Data();
        var asRecentData = yearly.GetMostRecent( ref data );
        Assert.IsFalse( asRecentData, $"New Yearly Value Data should return false for GetMostRecent()" );
    }
    
    [Test] public void YearlyValueDataCollectionSetWithDataStruct()
    {
        var yearly = new LongYearlyValueDataCollection();
        var data = new Data();
        data.year = FIRST_YEAR;
        data.value = DATA_A;
        yearly.TrySet( data , null);
        Assert.AreEqual( yearly.DataCount, 1, $"Setted with data struct YearlyValueDataCollection have 1 element but has {yearly.DataCount}" );
        var retrivedData = new Data();
        var asRecentData = yearly.GetMostRecent( ref retrivedData );
        Assert.IsTrue( asRecentData, $"Setted with data struct YearlyValueDataCollection should return true for GetMostRecent()" );
        Assert.AreEqual( retrivedData.year, FIRST_YEAR, $"MostRecent Setted Yearly Value Data year should be {FIRST_YEAR} but it is {retrivedData.year}" );
        Assert.AreEqual( retrivedData.value, DATA_A, $"MostRecent Setted Yearly Value Data year should be {DATA_A} but it is {retrivedData.value}" );
    }
    
    [Test] public void YearlyValueDataCollectionSetOuOfOrderIsOrdered()
    {
        var yearly = new LongYearlyValueDataCollection();
        var data1 = new Data();
        data1.year = FIRST_YEAR;
        data1.value = DATA_A;
        yearly.TrySet( data1, null );
        var data2 = new Data();
        data2.year = NEW_YEAR;
        data2.value = DATA_B;
        yearly.TrySet( data2, null );
        Assert.AreEqual( yearly.DataCount, 2, $"Double setted with data struct YearlyValueDataCollection have 2 element but has {yearly.DataCount}" );
        var retrivedData = new Data();
        var asRecentData = yearly.GetMostRecent( ref retrivedData );
        Assert.IsTrue( asRecentData, $"Double setted with data struct YearlyValueDataCollection should return true for GetMostRecent()" );
        Assert.AreEqual( retrivedData.year, NEW_YEAR, $"MostRecent Setted Yearly Value Data year should be {NEW_YEAR} but it is {retrivedData.year}" );
        Assert.AreEqual( retrivedData.value, DATA_B, $"MostRecent Setted Yearly Value Data year should be {DATA_B} but it is {retrivedData.value}" );
    }
    
    [Test] public void YearlyValueDataCollectionSetInOrderIsOrdered()
    {
        var yearly = new LongYearlyValueDataCollection();
        var data1 = new Data();
        data1.year = FIRST_YEAR;
        data1.value = DATA_A;
        var data2 = new Data();
        data2.year = NEW_YEAR;
        data2.value = DATA_B;
        yearly.TrySet( data2, null );
        yearly.TrySet( data1, null );
        Assert.AreEqual( yearly.DataCount, 2, $"Double setted with data struct YearlyValueDataCollection have 2 element but has {yearly.DataCount}" );
        var retrivedData = new Data();
        var asRecentData = yearly.GetMostRecent( ref retrivedData );
        Assert.IsTrue( asRecentData, $"Double setted with data struct YearlyValueDataCollection should return true for GetMostRecent()" );
        Assert.AreEqual( retrivedData.year, NEW_YEAR, $"MostRecent Setted Yearly Value Data year should be {NEW_YEAR} but it is {retrivedData.year}" );
        Assert.AreEqual( retrivedData.value, DATA_B, $"MostRecent Setted Yearly Value Data year should be {DATA_B} but it is {retrivedData.value}" );
    }
    
    [Test] public void YearlyValueDataCollectionSetSameYearDataShouldBeReplaced()
    {
        var yearly = new LongYearlyValueDataCollection();
        var data1 = new Data();
        data1.year = FIRST_YEAR;
        data1.value = DATA_A;
        var data2 = new Data();
        data2.year = FIRST_YEAR;
        data2.value = DATA_B;
        yearly.TrySet( data1, null );
        yearly.TrySet( data2, null );
        Assert.AreEqual( yearly.DataCount, 1, $"Double setted with data struct YearlyValueDataCollection have 1 element but has {yearly.DataCount}" );
        var retrivedData = new Data();
        var asRecentData = yearly.GetMostRecent( ref retrivedData );
        Assert.IsTrue( asRecentData, $"Double setted with data struct YearlyValueDataCollection should return true for GetMostRecent()" );
        Assert.AreEqual( retrivedData.year, FIRST_YEAR, $"MostRecent Setted Yearly Value Data year should be {FIRST_YEAR} but it is {retrivedData.year}" );
        Assert.AreEqual( retrivedData.value, DATA_B, $"MostRecent Setted Yearly Value Data year should be {DATA_B} but it is {retrivedData.value}" );
    }
    
    [Test] public void YearlyValueDataCollectionGetDataFromYear()
    {
        var yearly = new LongYearlyValueDataCollection();
        var data1 = new Data();
        data1.year = FIRST_YEAR;
        data1.value = DATA_A;
        var data2 = new Data();
        data2.year = NEW_YEAR;
        data2.value = DATA_B;
        yearly.TrySet( data1, null );
        yearly.TrySet( data2, null );

        long result = 0;
        Assert.IsTrue( yearly.GetDataFromYear( FIRST_YEAR, ref result ), $"Setted Yearly Value Data should find with GetDataFromYear( {FIRST_YEAR} )" );
        Assert.AreEqual( result, DATA_A, $"GetDataFromYear( {FIRST_YEAR} ) finding should result {DATA_A} but returned {result}" );
        Assert.IsTrue( yearly.GetDataFromYear( NEW_YEAR, ref result ), $"Setted Yearly Value Data should find with GetDataFromYear( {NEW_YEAR} )" );
        Assert.AreEqual( result, DATA_B, $"GetDataFromYear( {NEW_YEAR} ) finding should result {DATA_B} but returned {result}" );
        Assert.IsFalse( yearly.GetDataFromYear( OTHER_YEAR, ref result ), $"GetDataFromYear( {OTHER_YEAR} ) should not find any data" );
    }
}