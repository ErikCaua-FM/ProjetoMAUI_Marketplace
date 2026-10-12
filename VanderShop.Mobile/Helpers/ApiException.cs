using System;
using System.Collections.Generic;
using System.Text;

namespace VanderShop.Helpers;

public class ApiException(int status, string mensagem) : Exception(mensagem)
{
    public int Status { get; } = status;
}