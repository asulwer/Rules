using DynamicExpresso;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;

namespace Rules.Models
{
    public class Workflow
    {
        [Key]
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public ICollection<Rule> Rules { get; set; } = new List<Rule>();

        #region Obsolete

        public IEnumerable<T> ParseAsDelegate<T>(params string[] parameters)
        {
            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                foreach(var del in rule.ParseAsDelegate<T>(parameters))
                    yield return del;
            }
        }
        public IEnumerable<Expression<T>> ParseAsExpression<T>(params string[] parameters)
        {
            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                foreach (var del in rule.ParseAsExpression<T>(parameters))
                    yield return del;
            }
        }
        public IEnumerable<T> Eval<T>(params Parameter[] parameters)
        {
            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                foreach(var del in rule.Eval<T>(parameters))
                    yield return del;
            }
        }
        public IEnumerable<object> Eval(params Parameter[] parameters)
        {
            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                foreach (var del in rule.Eval(parameters))
                    yield return del;
            }
        }
        public IEnumerable<object> Invoke<T>(T t)
        {
            foreach (var rule in Rules.Where(r => r.IsActive))
            {
                foreach (var del in rule.Invoke(t))
                    yield return del;
            }
        }

        #endregion

        public IEnumerable<object> Execute(params Parameter[] parameters)
        {
            if (this.IsActive)
            {
                foreach (var rule in this.Rules)
                {
                    foreach (var del in rule.Execute(parameters))
                        yield return del;
                }
            }
        }
    }
}
