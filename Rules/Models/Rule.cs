using DynamicExpresso;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;

namespace Rules.Models
{
    public class Rule
    {
        private Interpreter interpreter => new Interpreter();

        [Key] public Guid Id { get; private set; } = Guid.NewGuid();
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string InExp { get; set; } = string.Empty;
        public string OutExp { get; set; } = string.Empty;
        
        public Guid? WorkflowId { get; set; }
        public Workflow? Workflow { get; set; } = null!;
        
        public Guid? ParentRuleId { get; set; }
        public Rule? ParentRule { get; set; } = null!;
        public ICollection<Rule> ChildRules { get; set; } = new List<Rule>();

        #region Obsolete

        public IEnumerable<T> ParseAsDelegate<T>(params string[] parameters)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.ParseAsDelegate<T>(parameters))
                    yield return del;                
            }

            yield return interpreter.ParseAsDelegate<T>(InExp, parameters);
        }
        public IEnumerable<Expression<T>> ParseAsExpression<T>(params string[] parameters)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.ParseAsExpression<T>(parameters))
                    yield return del;
            }

            yield return interpreter.ParseAsExpression<T>(InExp, parameters);
        }
        public IEnumerable<T> Eval<T>(params Parameter[] parameters)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.Eval<T>(parameters))
                    yield return del;
            }

            yield return interpreter.Eval<T>(InExp, parameters);
        }
        public IEnumerable<object> Eval(params Parameter[] parameters)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.Eval(parameters))
                    yield return del;
            }

            yield return interpreter.Eval(InExp, parameters);
        }
        public IEnumerable<object> Invoke<T>(T t)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.Invoke(t))
                    yield return del;
            }

            Parameter parameter = new Parameter(t!.GetType().Name, t!.GetType(), t);

            var exp = interpreter.Parse(InExp, parameter).Expression;
            Type[] ta = { parameter.Type, typeof(object) };
            var delegateType = System.Linq.Expressions.Expression.GetFuncType(ta);

            Type[] genericArguments = delegateType.GetGenericArguments();
            genericArguments[^1] = exp.Type;
            var le = System.Linq.Expressions.Expression.Lambda(delegateType.GetGenericTypeDefinition().MakeGenericType(genericArguments), exp, parameter.Expression);

            //see if this causes this to run faster
            //https://github.com/dadhi/FastExpressionCompiler?tab=readme-ov-file#how-to-use
            yield return le.Compile().DynamicInvoke(t)!;
        }

        #endregion

        public IEnumerable<object> Execute(params Parameter[] parameters)
        {
            if (this.IsActive)
            {
                if (string.IsNullOrEmpty(InExp) && string.IsNullOrEmpty(OutExp))
                    throw new Exception("Missing InExp or OutExp");
                else if (!string.IsNullOrEmpty(InExp) && !string.IsNullOrEmpty(OutExp))
                {
                    bool bFlag = interpreter.Eval<bool>(InExp, parameters);
                    if (bFlag)
                    {
                        foreach (var rule in ChildRules.Where(r => r.IsActive))
                        {
                            foreach (var del in rule.Execute(parameters))
                                yield return del;
                        }
                    }

                    yield return !bFlag ? bFlag : interpreter.Eval(OutExp, parameters);
                }
                else if (!string.IsNullOrEmpty(InExp) && string.IsNullOrEmpty(OutExp))
                {
                    bool bFlag = interpreter.Eval<bool>(InExp, parameters);
                    if (bFlag)
                    {
                        foreach (var rule in ChildRules.Where(r => r.IsActive))
                        {
                            foreach (var del in rule.Execute(parameters))
                                yield return del;
                        }
                    }

                    yield return bFlag;
                }
                else if (string.IsNullOrEmpty(InExp) && !string.IsNullOrEmpty(OutExp))
                {
                    foreach (var rule in ChildRules.Where(r => r.IsActive))
                    {
                        foreach (var del in rule.Execute(parameters))
                            yield return del;
                    }

                    yield return interpreter.Eval(OutExp, parameters);
                }
            }
        }
    }
}
