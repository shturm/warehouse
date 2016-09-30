using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class OperationNumbersUsage
    {
        [DbColumn (DataField.OperationType)]
        public OperationType OperationType { get; set; }

        [DbColumn (DataField.OperationLocationId)]
        public long LocationId { get; set; }

        [DbColumn ("LastUsedNumber")]
        public long LastUsedNumber { get; set; }

        [DbColumn ("UsedNumbers")]
        public long UsedNumbers { get; set; }

        /// <summary>
        /// Gets statistical info on how operation numbers are used
        /// </summary>
        /// <returns></returns>
        public static OperationNumbersUsage [] Get ()
        {
            return BusinessDomain.DataAccessProvider.GetOperationNumbersUsagePerLocation<OperationNumbersUsage> ();
        }

        public static ObjectsContainer<OperationType, long> [] GetUsagesStarts ()
        {
            return BusinessDomain.DataAccessProvider.GetOperationNumbersUsageStarts ();
        }
    }
}
