using System;
using System.Data.Entity;
using System.Linq.Expressions;
using Damasio34.Seedwork.Aggregates;
using Damasio34.Seedwork.UnitOfWork;

namespace Damasio34.GraphDiff
{
    /// <summary>
    ///     Cara que realiza a tarefa de atualizar um agregador no unitOfWork utilizando
    ///     o GraphDiff (https://github.com/refactorthis/GraphDiff)
    /// </summary>
    public class GraphdiffAggregateUpdateStrategy : OnlyAggregateRootUpdateStrategy
    {
        #region [ IAggregateUpdateStrategy ]

        public override void AlterarAgregacao<TAggRoot>(IUnitOfWork unitOfWork, TAggRoot aggRoot, Expression<Func<IAggregateConfiguration<TAggRoot>, object>> aggregateConfiguration)
        {
            // Recupera DbContext
            var dbContext = unitOfWork as DbContext;
            if (dbContext == null) throw new ArgumentException("Unit of work precisa ser um EntityFramework DbContext.");


            // se não foi informada a configuração da agregação, atualiza apenas o aggregate root. 
            if (aggregateConfiguration == null)
            {
                base.AlterarAgregacao(unitOfWork, aggRoot, null);
                return;
            }

            // Converte expressão
            var configExp = new UpdateGraphConfigurationBuilder<TAggRoot>().ConvertFrom(aggregateConfiguration);
            dbContext.UpdateGraph(aggRoot, configExp);
        }

        #endregion        
    }
}
