using Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain;
public interface IAppData
{
    public event Func<Task> Updated;

    public Task Add(ManualProxy manualProxy);
    public Task Delete(ManualProxy manualProxy);
    public Task<ManualProxy[]> Get();
    public Task<ManualProxy[]> Get(string dns);
}
