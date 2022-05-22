# Prototype-GPUAnimation_BoneTRS
将蒙皮动画支持GPUInstancing的一个方案。这是我参考(抄的)**UWA实现的方案**，自己重新实现了一遍**学习用的仓库**。  
功能只有播放和暂停动画，比较简单，作为一个方案的原型.

参考的文章:https://zhuanlan.zhihu.com/p/36896547    
参考的仓库:https://github.com/Unity-Technologies/Animation-Instancing  
## 原理
**采集信息**  
1.将动画每帧中角色的骨骼的模型空间RTS矩阵储存在贴图中.  
2.将每个动画信息(动画帧数，是否循环，动画信息在贴图中的起点)储存在ScriptableOBject中(或者其他序列化信息中)  
3.拷贝原来的Mesh，并将影响顶点权重的骨骼(最多四根)的Index和Weight存在UV2和UV3中(uv2.x = Index1，uv2.y = Weight1,uv2.z = Index2,uv2.w = Weight2以此类推)  
**动画播放**  
1.脚本:指定播放动画（设置材质的参数，动画在贴图中的位置）
2.脚本:根据时间，是否循环计算帧数(设置材质信息，正在播放哪一帧)
3.Shader(定点函数)：根据帧数以及动画，帧数，UV2,UV3采集贴图中骨骼(们)的索引权重，计算当前动画的顶点位置。
