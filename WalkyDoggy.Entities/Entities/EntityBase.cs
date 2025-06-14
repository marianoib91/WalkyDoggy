using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WalkyDoggy.Entities.Entities
{
   public abstract class EntityBase:IEntityBase
    {
        public Int64 Id { get; set; }

        public DateTime UpdatedDateTime { get; set; }

        public Int64 UpdatedBy { get; set; }

        public Int32 Status { get; set; }

        // Use deepCopy in true for cases in where the items of the details exist beyond the principal item. Example: Users with Roles.
        // Uses deepCopy in false when the items are created with the principal item. Example: Order and OrderItem.
        // Remember to check the UpdateEntityColllections method in RepositoryBase to update detail items
        public void CopyTo(EntityBase entityBase, Boolean deepCopy)
        {
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                if (deepCopy ||
                    (!deepCopy && !IsIEnumerable(pi)))
                {
                    object valueToCopy = pi.GetValue(this, null);

                    MethodInfo setMethod = pi.GetSetMethod();

                    if (setMethod != null)
                    {
                        pi.SetValue(entityBase, valueToCopy, null);
                    }
                }
            }
        }

        private Boolean IsIEnumerable(PropertyInfo pi)
        {
            return (pi.PropertyType != typeof(string) &&
                   pi.PropertyType.GetInterface(typeof(IEnumerable).Name) != null &&
                   pi.PropertyType.GetInterface(typeof(IEnumerable<>).Name) != null);
        }

        public EntityBase()
        {
            this.UpdatedDateTime = DateTime.Now;
            this.Status = 1;
        }
    }
}
