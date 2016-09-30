using Warehouse.Business.Operations;
using Warehouse.Data;

namespace Warehouse.Business.Entities
{
    public class OperationNumberingInfo
    {
        public const int MINIMAL_NUMBERS_PER_LOCATION = 10000;
        public const int RECOMMENDED_NUMBERS_PER_LOCATION = 1000000;

        [DbColumn (DataField.OperationNumber)]
        public long StartNumber { get; set; }

        [DbColumn (DataField.OperationType)]
        public OperationType OperationType { get; set; }

        [DbColumn (DataField.OperationLocationId)]
        public long LocationId { get; set; }

        public string UsageDescription { get; set; }

        public double Usage { get; set; }

        /// <summary>
        /// Gets the numbers to be used for documents (operations) per location.
        /// </summary>
        /// <returns>Value1 - LocationId, Value2 - Operation type, Value3 - Start number</returns>
        public static OperationNumberingInfo [] Get ()
        {
            return BusinessDomain.DataAccessProvider.GetOperationStartNumbersPerLocation<OperationNumberingInfo> ();
        }

        /// <summary>
        /// Creates for the first time the numbers to be used as ID-s for documents (<see cref="Operation"/>s).
        /// </summary>
        public static void Create ()
        {
            BusinessDomain.DataAccessProvider.CreateOperationStartNumbersPerLocation (MINIMAL_NUMBERS_PER_LOCATION, RECOMMENDED_NUMBERS_PER_LOCATION);
        }

        /// <summary>
        /// Updates the numbers used as ID-s for documents (<see cref="Operation"/>s).
        /// </summary>
        /// <param name="operations">The operations which contain the new values of the numbers.</param>
        public static void Update (OperationNumberingInfo [] operations)
        {
            BusinessDomain.DataAccessProvider.UpdateOperationStartNumbersPerLocation (operations);
        }

        /// <summary>
        /// Deletes the numbers used as ID-s for documents (<see cref="Operation"/>s) according to their location.
        /// </summary>
        public static void Delete ()
        {
            BusinessDomain.DataAccessProvider.DeleteOperationStartNumbersPerLocation ();
        }
    }
}
