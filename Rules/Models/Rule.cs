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
        private Interpreter Interpreter;

        public Rule() : this(new Interpreter()) { }
        public Rule(Interpreter interpreter) { Interpreter = interpreter; }
                
        [Key] public Guid Id { get; private set; } = Guid.NewGuid();
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public string InExp { get; set; } = string.Empty;
        public string OutExp { get; set; } = string.Empty;

        public IList<Parameter> LocalParameters { get; private set; } = new List<Parameter>();

        public Guid? WorkflowId { get; set; }
        public Workflow? Workflow { get; set; } = null!;
        
        public Guid? ParentRuleId { get; set; }
        public Rule? ParentRule { get; set; } = null!;
        public IList<Rule> ChildRules { get; set; } = new List<Rule>();

        public IEnumerable<object> Execute(params Parameter[] parameters)
        {
            if (this.IsActive)
            {
                //add parameters to this rule for one time use
                foreach(var p in parameters)
                    LocalParameters.Add(p);

                if (string.IsNullOrEmpty(InExp) && string.IsNullOrEmpty(OutExp))
                    throw new Exception("Missing InExp or OutExp");
                else if (!string.IsNullOrEmpty(InExp) && !string.IsNullOrEmpty(OutExp))
                {
                    bool bFlag = Interpreter.Eval<bool>(InExp, LocalParameters.ToArray());
                    if (bFlag)
                    {
                        foreach (var rule in ChildRules.Where(r => r.IsActive))
                        {
                            foreach (var del in rule.Execute(LocalParameters.ToArray()))
                                yield return del;
                        }
                    }

                    yield return !bFlag ? bFlag : Interpreter.Eval(OutExp, LocalParameters.ToArray()); //execute Action
                }
                else if (!string.IsNullOrEmpty(InExp) && string.IsNullOrEmpty(OutExp))
                {
                    bool bFlag = Interpreter.Eval<bool>(InExp, LocalParameters.ToArray());
                    if (bFlag)
                    {
                        foreach (var rule in ChildRules.Where(r => r.IsActive))
                        {
                            foreach (var del in rule.Execute(LocalParameters.ToArray()))
                                yield return del;
                        }
                    }

                    yield return bFlag;
                }
                else if (string.IsNullOrEmpty(InExp) && !string.IsNullOrEmpty(OutExp))
                {
                    foreach (var rule in ChildRules.Where(r => r.IsActive))
                    {
                        foreach (var del in rule.Execute(LocalParameters.ToArray()))
                            yield return del;
                    }

                    yield return Interpreter.Eval(OutExp, LocalParameters.ToArray());
                }

                //remove parameters after they have been used
                foreach (var p in parameters)
                    LocalParameters.Remove(p);
            }
        }

        #region Obsolete

        public IEnumerable<T> ParseAsDelegate<T>(params string[] parameters)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.ParseAsDelegate<T>(parameters))
                    yield return del;
            }

            yield return Interpreter.ParseAsDelegate<T>(InExp, parameters);
        }
        public IEnumerable<Expression<T>> ParseAsExpression<T>(params string[] parameters)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.ParseAsExpression<T>(parameters))
                    yield return del;
            }

            yield return Interpreter.ParseAsExpression<T>(InExp, parameters);
        }
        public IEnumerable<T> Eval<T>(params Parameter[] parameters)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.Eval<T>(parameters))
                    yield return del;
            }

            yield return Interpreter.Eval<T>(InExp, parameters);
        }
        public IEnumerable<object> Invoke<T>(T t)
        {
            foreach (var rule in ChildRules.Where(r => r.IsActive))
            {
                foreach (var del in rule.Invoke(t))
                    yield return del;
            }

            Parameter parameter = new Parameter(t!.GetType().Name, t!.GetType(), t);

            var exp = Interpreter.Parse(InExp, parameter).Expression;
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
    }
}
