using Alpaca.Markets;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradingBot.Models;
using TradingBot.Services.AlpacaClients;
using TradingBot.Services;
using Microsoft.AspNetCore.Authentication;
using TradingBot.Configuration;
using FluentAssertions.Common;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace TradingBotTests
{
    [Trait("Category", "Unit")]
    public sealed class StrategyTests
    {
        private readonly IDictionary<TradingSymbol, Prediction> _predictions;
        private readonly Assets _assets;
        private readonly StrategyParametersConfiguration _strategyParameters;
        public StrategyTests()
        {
            _predictions = new Dictionary<TradingSymbol, Prediction>
            {
                [new TradingSymbol("TSLA")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 238.405077066421508778432m,
                            HighPrice = 246.66130756638944149024464m,
                            LowPrice = 233.48703374415636063042m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 238.11419290368937064966644686m,
                            HighPrice = 246.40406112291012496019389897m,
                            LowPrice = 233.48073286238147814750750690m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 238.12185341992876140069643655m,
                            HighPrice = 246.14116784587022328943201235m,
                            LowPrice = 233.67585574587523818735511651m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 238.18518688600698779117844094m,
                            HighPrice = 245.77157939747751975504611287m,
                            LowPrice = 233.66958545947559976052541495m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 237.93334641874799449356894287m,
                            HighPrice = 245.27658628854233925619820412m,
                            LowPrice = 233.65393584347483400375150561m
                        }
                    }
                },
                [new TradingSymbol("NIO")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 7.46217562213540077491m,
                            HighPrice = 8.02069531574845314008m,
                            LowPrice = 7.473686045184731484m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 7.4926335673697204516182797691m,
                            HighPrice = 8.054461448946673468548657847m,
                            LowPrice = 7.5239831276303308858054722239m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 7.5430676826211577834161688843m,
                            HighPrice = 8.090461546393334813089373669m,
                            LowPrice = 7.5845253933134998013044918424m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 7.5974439377532792854344175959m,
                            HighPrice = 8.119359666912070476908094919m,
                            LowPrice = 7.6390317148951293378750554930m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 7.6190147719066885375557047671m,
                            HighPrice = 8.138034207793401914028881873m,
                            LowPrice = 7.6896533934668401714282031821m
                        }
                    }
                },
                [new TradingSymbol("SQQQ")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 8.12357616213312017021m,
                            HighPrice = 8.527992312876878697722m,
                            LowPrice = 8.1067943656515040743771m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 8.1641297470028437277643120719m,
                            HighPrice = 8.576308344824195719881877938m,
                            LowPrice = 8.1495880674654770257771577059m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 8.1840208545133462464653236978m,
                            HighPrice = 8.6134985303384282483085154636m,
                            LowPrice = 8.1765111349476829033087068934m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 8.2066708421381879289809227195m,
                            HighPrice = 8.6499134090387411370437221698m,
                            LowPrice = 8.1958305420377850068734777436m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 8.2198442132428926477075113361m,
                            HighPrice = 8.6795715473347770958659332668m,
                            LowPrice = 8.2109294052910364257440753988m
                        }
                    }
                },
                [new TradingSymbol("PLTR")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 18.2405069500207901031m,
                            HighPrice = 18.58689469769597053680m,
                            LowPrice = 18.04737931445240973744m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 18.180531060955260885678079427m,
                            HighPrice = 18.541046248011779853056879342m,
                            LowPrice = 18.015282551789223573729321982m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 18.150069012691606886327553260m,
                            HighPrice = 18.498729344114006761575182314m,
                            LowPrice = 18.005381428108497821418762905m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 18.127541491053624937680359407m,
                            HighPrice = 18.446485881506187569118843983m,
                            LowPrice = 17.982607494900796591920172425m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 18.082114631978295184579557901m,
                            HighPrice = 18.387993437531151091619367380m,
                            LowPrice = 17.961775546866026527726594513m
                        }
                    }
                },
                [new TradingSymbol("TQQQ")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 43.12433634482324123019m,
                            HighPrice = 43.8338337596505880317m,
                            LowPrice = 42.544206863354891530077m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 42.955410717555847969189390198m,
                            HighPrice = 43.700324458444275547750099400m,
                            LowPrice = 42.446436262693498285735877102m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 42.863453474391562795494910215m,
                            HighPrice = 43.576685643323927901217972315m,
                            LowPrice = 42.401438388740100151292728922m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 42.785355048735137995959149178m,
                            HighPrice = 43.431188282691044477012092507m,
                            LowPrice = 42.331052136378193958970409040m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 42.662343795623971243332447080m,
                            HighPrice = 43.273334338773817555495998340m,
                            LowPrice = 42.266242614807553998472429378m
                        }
                    }
                },
                [new TradingSymbol("RIVN")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 17.46731238905340432881m,
                            HighPrice = 18.08719598822295665792m,
                            LowPrice = 17.33676621382236480773680m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 17.530683548895460849206764717m,
                            HighPrice = 18.146055027355105822222000477m,
                            LowPrice = 17.421422913514883131441534144m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 17.615906291388164655922362439m,
                            HighPrice = 18.205710888886306313927954544m,
                            LowPrice = 17.524637700308795569872969166m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 17.711127113472828621991104945m,
                            HighPrice = 18.250263141721911492732430187m,
                            LowPrice = 17.611858327149637537540449849m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 17.747973400205633975562292465m,
                            HighPrice = 18.275556609835445439219116007m,
                            LowPrice = 17.689930489448278415094819671m
                        }
                    }
                },
                [new TradingSymbol("SOXL")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 21.80234430905431509654m,
                            HighPrice = 22.1405805369094014170460m,
                            LowPrice = 21.47136267766319214933m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 21.668901859339981512038975677m,
                            HighPrice = 22.025533018585036867200607840m,
                            LowPrice = 21.382473938767620727331444134m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 21.577992641866242220805511229m,
                            HighPrice = 21.916405994563576147031375090m,
                            LowPrice = 21.322853472008866272072013826m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 21.492591330734589627311118524m,
                            HighPrice = 21.798318867154330201012781004m,
                            LowPrice = 21.257136205295351921401294875m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 21.401331301584409372618146549m,
                            HighPrice = 21.682937593748549165106891918m,
                            LowPrice = 21.194033339386697606433220633m
                        }
                    }
                },
                [new TradingSymbol("AMD")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 118.213503587767481806368m,
                            HighPrice = 118.4797897534444928174203m,
                            LowPrice = 116.407818045467138287585m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 118.06894281282374670608970932m,
                            HighPrice = 118.36223483848224428908018064m,
                            LowPrice = 116.38849880809364631130773735m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 118.06404210343004991303101769m,
                            HighPrice = 118.23649862975910616600153879m,
                            LowPrice = 116.46403988189776748667738680m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 118.09335849420669951684721027m,
                            HighPrice = 118.06334820396979708797433255m,
                            LowPrice = 116.43849710330160853749785378m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 117.95613533137624816104868271m,
                            HighPrice = 117.82532716305958302912531261m,
                            LowPrice = 116.40711404611923739824300910m
                        }
                    }
                },
                [new TradingSymbol("MARA")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 15.21453737735748290944m,
                            HighPrice = 16.11181389715522528007m,
                            LowPrice = 14.94643731046468019027m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 15.146090751052113394769092583m,
                            HighPrice = 16.048618792943519382116672723m,
                            LowPrice = 14.911924286693271576293038894m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 15.109282148337061259257107954m,
                            HighPrice = 15.990700377599889361307735713m,
                            LowPrice = 14.897647005182265353476333162m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 15.077378700127566209560365751m,
                            HighPrice = 15.929167247235234054589735043m,
                            LowPrice = 14.876228805435325159079734649m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 15.032188710406313313534653017m,
                            HighPrice = 15.863840355948434055225398229m,
                            LowPrice = 14.853337789897704412990019794m
                        }
                    }
                },
                [new TradingSymbol("SOXS")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 8.84562028538435697771m,
                            HighPrice = 8.91851251438260078849m,
                            LowPrice = 8.678417233526706695480m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 8.903697919347435014042585462m,
                            HighPrice = 8.962905092105986281565648735m,
                            LowPrice = 8.755507511791212608209943330m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 8.968830116108417988813761690m,
                            HighPrice = 9.010568393820741947996685110m,
                            LowPrice = 8.845418448010368229598862418m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 9.047150805336194700975044031m,
                            HighPrice = 9.048751609047884491182432785m,
                            LowPrice = 8.919394635919414626896333669m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 9.081859470502282815185807552m,
                            HighPrice = 9.075213823844223153619516471m,
                            LowPrice = 8.990668198369737698531201804m
                        }
                    }
                },
                [new TradingSymbol("ALT")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 5.18455016396939754374m,
                            HighPrice = 5.282210071384906764m,
                            LowPrice = 4.4714854140579700506m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 5.2004008173449600517008287384m,
                            HighPrice = 5.2936813037468273778254836186m,
                            LowPrice = 4.4978730216403212849721034878m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 5.2338885055095297282973272696m,
                            HighPrice = 5.3096251501921627372017464370m,
                            LowPrice = 4.5285812263159578290162755218m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 5.2662146313947517418087848062m,
                            HighPrice = 5.3207975865583342789910774731m,
                            LowPrice = 4.5584162316284961257210403204m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 5.2755736402867213535954648160m,
                            HighPrice = 5.3241788992665028047374073118m,
                            LowPrice = 4.5838734406030629677899764924m
                        }
                    }
                },
                [new TradingSymbol("AAL")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 13.122869307138025760884m,
                            HighPrice = 13.30437477149069309367m,
                            LowPrice = 13.023197279591113327905m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 13.118540749970592848347692516m,
                            HighPrice = 13.303427183027356378033074028m,
                            LowPrice = 13.031995121263249260661454895m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 13.130247745464276645937947625m,
                            HighPrice = 13.301856158662743503598747159m,
                            LowPrice = 13.052444155242622898184277763m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 13.147446391569293020150030046m,
                            HighPrice = 13.293358762945810017836310954m,
                            LowPrice = 13.061635431231637729277388495m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 13.140359022194019305798451454m,
                            HighPrice = 13.276759016733234577180390930m,
                            LowPrice = 13.069146388006291025350618433m
                        }
                    }
                },
                [new TradingSymbol("HOOD")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 10.58718823537230491922m,
                            HighPrice = 10.67123890910856425810590m,
                            LowPrice = 9.676070116013288498238m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 10.647087162029747286046382440m,
                            HighPrice = 10.725228683212935837666936674m,
                            LowPrice = 9.749597634537874331905979299m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 10.722999156183365711072008679m,
                            HighPrice = 10.779513906364332681078776297m,
                            LowPrice = 9.831996422274446369462562919m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 10.804433212818584165717550425m,
                            HighPrice = 10.823926424104236252585112452m,
                            LowPrice = 9.904739483572821646919123591m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 10.841241880199027048350035422m,
                            HighPrice = 10.853675427384570604063712976m,
                            LowPrice = 9.972587248883445706841678466m
                        }
                    }
                },
                [new TradingSymbol("TLT")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 94.6047707035765051840738m,
                            HighPrice = 94.709240535646677014292m,
                            LowPrice = 93.6860764282383024712820m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 94.61879143559523069914940246m,
                            HighPrice = 94.74068662446164010778434714m,
                            LowPrice = 93.79646481331676374800496872m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 94.74716272977520779613930468m,
                            HighPrice = 94.77033258969166370388984791m,
                            LowPrice = 93.99177644073649012324840352m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 94.91843725567757648456155201m,
                            HighPrice = 94.74567649326527029071703301m,
                            LowPrice = 94.10189020999693525376163780m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 94.90320174704925512018138983m,
                            HighPrice = 94.66268897261356652344599850m,
                            LowPrice = 94.19834698132952436492018831m
                        }
                    }
                },
                [new TradingSymbol("PLUG")] = new Prediction
                {
                    Prices = new List<DailyPricePrediction>
                    {
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 06),
                            ClosingPrice = 4.26420837104320526104m,
                            HighPrice = 4.64825740277767181508m,
                            LowPrice = 4.15417857989668846092m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 07),
                            ClosingPrice = 4.2896781226174725387631285787m,
                            HighPrice = 4.6683309800042517384589963733m,
                            LowPrice = 4.1908390217179553262331446513m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 08),
                            ClosingPrice = 4.3196956549982462765548924506m,
                            HighPrice = 4.6892449490091341375174726442m,
                            LowPrice = 4.2327473719681794877023181043m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 11),
                            ClosingPrice = 4.3549842587443680793529526281m,
                            HighPrice = 4.7067070571748813011524673335m,
                            LowPrice = 4.2689696529518492280703033547m
                        },
                        new DailyPricePrediction
                        {
                            Date = new DateOnly(2023, 12, 12),
                            ClosingPrice = 4.3701517653736764054418554314m,
                            HighPrice = 4.7178748572754473113198441748m,
                            LowPrice = 4.3023741744064084958687795963m
                        }
                    }
                }
            };
            _assets = new Assets
            {
                EquityValue = 12345m,
                Cash = new Cash
                {
                    AvailableAmount = 1234m,
                    MainCurrency = "EUR"
                },
                Positions = new Dictionary<TradingSymbol, Position>
                {
                    [new TradingSymbol("MARA")] = new Position
                    {
                        SymbolId = Guid.NewGuid(),
                        Symbol = new TradingSymbol("MARA"),
                        Quantity = 100m,
                        AvailableQuantity = 80m,
                        AverageEntryPrice = 234m,
                        MarketValue = 23456m
                    },
                    [new TradingSymbol("PLUG")] = new Position
                    {
                        SymbolId = Guid.NewGuid(),
                        Symbol = new TradingSymbol("PLUG"),
                        Quantity = 120m,
                        AvailableQuantity = 90m,
                        AverageEntryPrice = 9.81m,
                        MarketValue = 1357m
                    }
                }
            };
            _strategyParameters = new StrategyParametersConfiguration
            {
                MaxStocksBuyCount = 5,
                MinDaysDecreasing = 3,
                MinDaysIncreasing = 3,
                TopGrowingSymbolsBuyRatio = 0.4m
            };
        }
        [Fact]
        public async Task ShouldCorrectlyGenerateTradeActions()
        {
            var clock = Substitute.For<ISystemClock>();
            var time = DateTime.UtcNow;
            clock.UtcNow.Returns(time);
            var today = DateOnly.FromDateTime(time);

            var predictior = Substitute.For<IPricePredictor>();
            predictior.GetPredictionsAsync().Returns(_predictions);

            var assetsData = Substitute.For<IAssetsDataSource>();
            assetsData.GetAssetsAsync().Returns(_assets);

            var marketData = Substitute.For<IMarketDataSource>();
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("RIVN"), today, today).Returns(new List<DailyTradingData>
            { 
                new DailyTradingData
                {
                    Date = today,
                    Open = 17.36731238905340432881m,
                    Close = 17.36731238905340432881m,
                    High = 17.36731238905340432881m,
                    Low = 17.36731238905340432881m,
                    Volume = 17.36731238905340432881m
                } 
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("TSLA"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 238.405077066421508778432m,
                    Close = 238.405077066421508778432m,
                    High = 238.405077066421508778432m,
                    Low = 238.405077066421508778432m,
                    Volume = 238.405077066421508778432m
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("NIO"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 7.46217562213540077491m,
                    Close = 7.46217562213540077491m,
                    High = 7.46217562213540077491m,
                    Low = 7.46217562213540077491m,
                    Volume = 7.46217562213540077491m,
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("SQQQ"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 8.12357616213312017021m,
                    Close = 8.12357616213312017021m,
                    High = 8.12357616213312017021m,
                    Low = 8.12357616213312017021m,
                    Volume = 8.12357616213312017021m,
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("PLTR"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 43.12433634482324123019m,
                    Close = 43.12433634482324123019m,
                    High = 43.12433634482324123019m,
                    Low = 43.12433634482324123019m,
                    Volume = 43.12433634482324123019m
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("TQQQ"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 43.12433634482324123019m,
                    Close = 43.12433634482324123019m,
                    High = 43.12433634482324123019m,
                    Low = 43.12433634482324123019m,
                    Volume = 43.12433634482324123019m
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("SOXL"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 21.80234430905431509654m,
                    Close = 21.80234430905431509654m,
                    High = 21.80234430905431509654m,
                    Low = 21.80234430905431509654m,
                    Volume = 21.80234430905431509654m
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("AMD"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 118.213503587767481806368m,
                    Close = 118.213503587767481806368m,
                    High = 118.213503587767481806368m,
                    Low = 118.213503587767481806368m,
                    Volume = 118.213503587767481806368m
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("MARA"), today, today).Returns(new List<DailyTradingData>
            {
                new DailyTradingData
                {
                    Date = today,
                    Open = 15.21453737735748290944m,
                    Close = 15.21453737735748290944m,
                    High = 15.21453737735748290944m,
                    Low = 15.21453737735748290944m,
                    Volume = 15.21453737735748290944m
                }
            });
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("SOXS"), today, today).Returns((List<DailyTradingData>?)null);
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("ALT"), today, today).Returns((List<DailyTradingData>?)null);
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("AAL"), today, today).Returns((List<DailyTradingData>?)null);
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("HOOD"), today, today).Returns((List<DailyTradingData>?)null);
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("TLT"), today, today).Returns((List<DailyTradingData>?)null);
            marketData.GetDataForSingleSymbolAsync(new TradingSymbol("PLUG"), today, today).Returns((List<DailyTradingData>?)null);

            var strategyParameters = Substitute.For<IStrategyParametersService>();
            strategyParameters.GetConfigurationAsync().Returns(_strategyParameters);

            var strategy = new Strategy(predictior, assetsData, marketData, clock, strategyParameters);
            var tradeActions = await strategy.GetTradingActionsAsync();
            tradeActions.Should().ContainEquivalentOf<TradingAction>(new TradingAction
            {
                Id = Arg.Any<Guid>(),
                CreatedAt = time,
                Price = null,
                Quantity = 80m,
                Symbol = new TradingSymbol("MARA"),
                InForce = TimeInForce.Day,
                OrderType = TradingBot.Models.OrderType.MarketSell
            }, options => options
                .Excluding(x => x.Id));
        }
    }
}
