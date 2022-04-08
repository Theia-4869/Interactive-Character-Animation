// !!!!!!!!!!!!!!!!!!!
// 姓名：张启哲
// 学号：1900011638
// !!!!!!!!!!!!!!!!!!!
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class keyboard_control : MonoBehaviour
{
    // 用到的bvh文件路径
    private string walk_bvh_fname = "Assets\\BVH\\walk.bvh";
    private string cartwheel_bvh_fname = "Assets\\BVH\\cartwheel.bvh";
    // 从bvh文件中读取所有的关节名称，需要用关节名称来找unity scene中的game object
    private List<string> joints = new List<string>();
    private List<string> obstacles = new List<string>();
    // root 结点 game object
    private GameObject root;
    // game object列表
    private List<GameObject> gameObjects = new List<GameObject>();
    private List<GameObject> obstacleObjects = new List<GameObject>();
    // 时间戳
    private int time_step = 0;
    // 记录自动播放模式的时间戳 (用于切换自动/手动模式)
    private int auto_time_step = 0;
    // bvh的帧数
    private int walk_frame_num = 0;
    private int cartwheel_frame_num = 0;
    // 旋转持续帧数
    private int rotation_frame_num = 60;
    // 插值持续帧数
    private int interpolation_frame_num = 30;

    // ! 在这里声明你需要的其他数据结构
    // 父结点列表
    private List<int> fatherNodes = new List<int>();
    // 栈用于记录树结构层次
    private Stack<int> nowLevel = new Stack<int>();
    // offset列表
    private List<Vector3> offsets = new List<Vector3>();
    // root结点的position channels
    private string[] positionChannel = new string[3];
    // 各结点rotation channels
    private List<string[]> channels = new List<string[]>();

    // motion部分root结点的position解析
    private List<Vector3> walk_positions = new List<Vector3>();
    private List<Vector3> cartwheel_positions = new List<Vector3>();
    // motion部分各结点的rotation解析
    private List<List<Quaternion>> walk_rotations = new List<List<Quaternion>>();
    private List<List<Quaternion>> cartwheel_rotations = new List<List<Quaternion>>();
    // 记录root结点的全局位置偏移
    private Vector3 root_position = new Vector3(1.0F, 0.0F, 0.0F);
    // 记录自动播放模式下root结点的全局位置偏移 (用于切换自动/手动模式)
    private Vector3 auto_root_position = new Vector3(0.0F, 0.0F, 0.0F);
    // 记录root结点的全局旋转偏移
    private Quaternion root_rotation = Quaternion.identity;
    // 记录自动播放模式下root结点的全局旋转偏移 (用于切换自动/手动模式)
    private Quaternion auto_root_rotation = Quaternion.identity;
    // 自动播放控制符
    private bool autoPlayFlag = true;
    // 自动/手动模式切换延迟
    private int autoPlayInterval = 0;
    // 碰撞检测标志
    private bool collision_flag = false;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        nowLevel.Push(-1);  // 为root结点先推入-1标记

        // 处理walk.bvh文件
        StreamReader walk_bvh_file = new StreamReader(new FileStream(walk_bvh_fname, FileMode.Open));
        while (!walk_bvh_file.EndOfStream)
        {
            string line = walk_bvh_file.ReadLine();
            string str = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(line, " ");
            string[] split_line = str.Split(' ');
            // 处理bvh文件中的character hierarchy部分
            if (line.Contains("ROOT") || line.Contains("JOINT"))
            {
                // ! 处理这一行的信息
                // 记录joint名称及父结点
                joints.Add(split_line[split_line.Length - 1]);
                fatherNodes.Add(nowLevel.Peek());

                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("End Site"))
            {
                // ! 处理这一行的信息


                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("{"))
            {
                // ! 处理这一行的信息
                // 子树开始, 父结点号入栈
                nowLevel.Push(joints.Count - 1);

                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("}"))
            {
                // ! 处理这一行的信息
                // 当前子树结束, 父结点号出栈
                nowLevel.Pop();

                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("OFFSET"))
            {
                // ! 处理这一行的信息
                // 记录offset值, 顺序一般为 (X, Y, Z)
                offsets.Add(new Vector3(float.Parse(split_line[2]), float.Parse(split_line[3]), float.Parse(split_line[4])));

                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("CHANNELS"))
            {
                // ! 处理这一行的信息
                if (split_line[2] == "3")
                {         // 记录3通道rotation channel
                    channels.Add(new string[3] { split_line[3], split_line[4], split_line[5] });
                }
                else if (split_line[2] == "6")
                {    // 记录6通道position channel + rotation channel
                    positionChannel = new string[3] { split_line[3], split_line[4], split_line[5] };
                    channels.Add(new string[3] { split_line[6], split_line[7], split_line[8] });
                }

                // ! 在以上空白写下你的代码
            }
            else if (line.Contains("Frame Time"))
            {
                // Frame Time是数据部分前的最后一行，读到这一行后跳出循环
                break;
            }
            else if (line.Contains("Frames:"))
            {
                // 获取帧数
                walk_frame_num = int.Parse(split_line[split_line.Length - 1]);
            }
        }
        // 接下来处理walk.bvh文件中的数据部分
        while (!walk_bvh_file.EndOfStream)
        {
            string line = walk_bvh_file.ReadLine();
            string str = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(line, " ");
            string[] split_line = str.Split(' ');
            // ! 解析每一行数据，保存在合适的数据结构中，用于之后update
            // 注意数据的顺序是和之前的channel顺序对应的
            // 提示：欧拉角顺序可能有多种，但都可以用三个四元数相乘得到，注意相乘的顺序
            // 按照之前存储的channels将position与rotation都转换为 (X, Y, Z) 的顺序并记录
            Dictionary<string, float> positionSF = new Dictionary<string, float>();
            Dictionary<string, float> rotationSF = new Dictionary<string, float>();
            List<Quaternion> rotationRow = new List<Quaternion>();
            for (int i = 1; i < 4; i++)
            {
                positionSF[positionChannel[i - 1]] = float.Parse(split_line[i]);
            }
            walk_positions.Add(new Vector3(positionSF["Xposition"], positionSF["Yposition"], positionSF["Zposition"]));
            for (int i = 4; i < split_line.Length; i += 3)
            {
                rotationSF[channels[(i - 4) / 3][0]] = float.Parse(split_line[i]);
                rotationSF[channels[(i - 4) / 3][1]] = float.Parse(split_line[i + 1]);
                rotationSF[channels[(i - 4) / 3][2]] = float.Parse(split_line[i + 2]);
                // 欧拉角转换为四元数
                rotationRow.Add(Quaternion.Euler(new Vector3(rotationSF["Xrotation"], rotationSF["Yrotation"], rotationSF["Zrotation"])));
            }
            walk_rotations.Add(rotationRow);

            // ! 在以上空白写下你的代码
        }

        // 处理cartwheel.bvh文件
        StreamReader cartwheel_bvh_file = new StreamReader(new FileStream(cartwheel_bvh_fname, FileMode.Open));
        while (!cartwheel_bvh_file.EndOfStream)
        {
            string line = cartwheel_bvh_file.ReadLine();
            string str = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(line, " ");
            string[] split_line = str.Split(' ');
            if (line.Contains("Frame Time"))
            {
                // Frame Time是数据部分前的最后一行，读到这一行后跳出循环
                break;
            }
            else if (line.Contains("Frames:"))
            {
                // 获取帧数
                cartwheel_frame_num = int.Parse(split_line[split_line.Length - 1]);
            }
        }
        // 接下来处理cartwheel.bvh文件中的数据部分
        while (!cartwheel_bvh_file.EndOfStream)
        {
            string line = cartwheel_bvh_file.ReadLine();
            string str = new System.Text.RegularExpressions.Regex("[\\s]+").Replace(line, " ");
            string[] split_line = str.Split(' ');
            // ! 解析每一行数据，保存在合适的数据结构中，用于之后update
            // 注意数据的顺序是和之前的channel顺序对应的
            // 提示：欧拉角顺序可能有多种，但都可以用三个四元数相乘得到，注意相乘的顺序
            // 按照之前存储的channels将position与rotation都转换为 (X, Y, Z) 的顺序并记录
            Dictionary<string, float> positionSF = new Dictionary<string, float>();
            Dictionary<string, float> rotationSF = new Dictionary<string, float>();
            List<Quaternion> rotationRow = new List<Quaternion>();
            for (int i = 1; i < 4; i++)
            {
                positionSF[positionChannel[i - 1]] = float.Parse(split_line[i]);
            }
            cartwheel_positions.Add(new Vector3(positionSF["Xposition"], positionSF["Yposition"], positionSF["Zposition"]));
            for (int i = 4; i < split_line.Length; i += 3)
            {
                rotationSF[channels[(i - 4) / 3][0]] = float.Parse(split_line[i]);
                rotationSF[channels[(i - 4) / 3][1]] = float.Parse(split_line[i + 1]);
                rotationSF[channels[(i - 4) / 3][2]] = float.Parse(split_line[i + 2]);
                // 欧拉角转换为四元数
                rotationRow.Add(Quaternion.Euler(new Vector3(rotationSF["Xrotation"], rotationSF["Yrotation"], rotationSF["Zrotation"])));
            }
            cartwheel_rotations.Add(rotationRow);

            // ! 在以上空白写下你的代码
        }

        // 按关节名称获取所有的game object
        root = GameObject.Find("RootJoint");
        GameObject tmp_obj = new GameObject();
        for (int i = 0; i < joints.Count; i++)
        {
            tmp_obj = GameObject.Find(joints[i]);
            gameObjects.Add(tmp_obj);
        }
        auto_root_position = root.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 joint_position = new Vector3(0.0F, 0.0F, 0.0F);
        Quaternion joint_orientation = Quaternion.identity;
        // ! 定义你需要的局部变量
        // 当前帧的rotations
        List<Quaternion> rotationRow = new List<Quaternion>();

        // ! 在以上空白写下你的代码
        // 自动/手动模式切换延迟衰减
        if (autoPlayInterval > 0)
        {
            autoPlayInterval -= 1;
        }

        if (autoPlayFlag)   // 自动模式
        {
            if (time_step < walk_frame_num)
            {   // 先向前直走
                rotationRow = walk_rotations[time_step];
                root.transform.position = root_position + walk_positions[time_step];
            }
            else if (time_step < walk_frame_num + rotation_frame_num)
            {   // 转身
                rotationRow = walk_rotations[walk_frame_num - 1];
                root_rotation = root_rotation * Quaternion.Euler(0.0F, -3.0F, 0.0F);
                if (time_step == walk_frame_num + rotation_frame_num - 1)
                {
                    root_position = new Vector3(root.transform.position.x, 0.0F, root.transform.position.z) -
                                    new Vector3(cartwheel_positions[0].x, 0.0F, cartwheel_positions[0].z);
                    root_rotation = root_rotation * Quaternion.Inverse(cartwheel_rotations[0][0]);
                }
            }
            else if (time_step < walk_frame_num + rotation_frame_num + interpolation_frame_num)
            {   // 直走与翻滚之间的Slerp插值过渡
                float t = (time_step - walk_frame_num - rotation_frame_num) * 1.0F / (interpolation_frame_num - 1);
                rotationRow.Clear();
                for (int i = 0; i < cartwheel_rotations[0].Count; i++)
                {
                    if (i == 0)
                    {
                        rotationRow.Add(Quaternion.Slerp(cartwheel_rotations[0][0] * walk_rotations[walk_frame_num - 1][0], cartwheel_rotations[0][0], t));
                    }
                    else
                    {
                        rotationRow.Add(Quaternion.Slerp(walk_rotations[walk_frame_num - 1][i], cartwheel_rotations[0][i], t));
                    }
                }
            }
            else if (time_step < walk_frame_num + cartwheel_frame_num + rotation_frame_num + interpolation_frame_num)
            {   // 翻滚一周
                rotationRow = cartwheel_rotations[time_step - walk_frame_num - rotation_frame_num - interpolation_frame_num];
                root.transform.position = root_position + cartwheel_positions[time_step - walk_frame_num - rotation_frame_num - interpolation_frame_num];
                if (time_step == walk_frame_num + cartwheel_frame_num + rotation_frame_num + interpolation_frame_num - 1)
                {
                    root_position = new Vector3(root.transform.position.x, 0.0F, root.transform.position.z) +
                                    new Vector3(walk_positions[0].x, 0.0F, walk_positions[0].z);
                    root_rotation = Quaternion.Euler(0.0F, -180.0F, 0.0F);
                }
            }
            else if (time_step < walk_frame_num * 2 + cartwheel_frame_num + rotation_frame_num + interpolation_frame_num)
            {   // 继续直走
                rotationRow = walk_rotations[time_step - walk_frame_num - cartwheel_frame_num - rotation_frame_num - interpolation_frame_num];
                Vector3 next_position = walk_positions[time_step - walk_frame_num - cartwheel_frame_num - rotation_frame_num - interpolation_frame_num];
                root.transform.position = root_position + new Vector3(-next_position.x, next_position.y, -next_position.z);
            }
            else if (time_step < walk_frame_num * 2 + cartwheel_frame_num + rotation_frame_num * 2 + interpolation_frame_num)
            {   // 转身
                rotationRow = walk_rotations[walk_frame_num - 1];
                root_rotation = root_rotation * Quaternion.Euler(0.0F, -3.0F, 0.0F);
                if (time_step == walk_frame_num * 2 + cartwheel_frame_num + rotation_frame_num * 2 + interpolation_frame_num - 1)
                {
                    root_position = new Vector3(root.transform.position.x, 0.0F, root.transform.position.z) +
                                    new Vector3(cartwheel_positions[0].x, 0.0F, cartwheel_positions[0].z);
                    root_rotation = root_rotation * Quaternion.Inverse(cartwheel_rotations[0][0]);
                }
            }
            else if (time_step < walk_frame_num * 2 + cartwheel_frame_num + rotation_frame_num * 2 + interpolation_frame_num * 2)
            {   // 直走与翻滚之间的Slerp插值过渡
                float t = (time_step - walk_frame_num * 2 - cartwheel_frame_num - rotation_frame_num * 2 - interpolation_frame_num) * 1.0F / (interpolation_frame_num - 1);
                rotationRow.Clear();
                for (int i = 0; i < cartwheel_rotations[0].Count; i++)
                {
                    if (i == 0)
                    {
                        rotationRow.Add(Quaternion.Slerp(cartwheel_rotations[0][0] * walk_rotations[walk_frame_num - 1][0], cartwheel_rotations[0][0], t));
                    }
                    else
                    {
                        rotationRow.Add(Quaternion.Slerp(walk_rotations[walk_frame_num - 1][i], cartwheel_rotations[0][i], t));
                    }
                }
            }
            else if (time_step < walk_frame_num * 2 + cartwheel_frame_num * 2 + rotation_frame_num * 2 + interpolation_frame_num * 2)
            {   // 再翻滚一周, 回到原点
                rotationRow = cartwheel_rotations[time_step - walk_frame_num * 2 - cartwheel_frame_num - rotation_frame_num * 2 - interpolation_frame_num * 2];
                Vector3 next_position = cartwheel_positions[time_step - walk_frame_num * 2 - cartwheel_frame_num - rotation_frame_num * 2 - interpolation_frame_num * 2];
                root.transform.position = root_position + new Vector3(-next_position.x, next_position.y, -next_position.z);
                if (time_step == walk_frame_num * 2 + cartwheel_frame_num * 2 + rotation_frame_num * 2 + interpolation_frame_num * 2 - 1)
                {
                    root_position = new Vector3(1.0F, 0.0F, 0.0F);
                    root_rotation = Quaternion.identity;
                }
            }
            time_step = (time_step + 1)%(walk_frame_num*2 + cartwheel_frame_num*2 + rotation_frame_num*2 + interpolation_frame_num*2);
        }
        else    // 手动模式, 用 GetKey 或 GetKeyDown 或 GetKeyUp 交互
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                // 当按下 W 键或 ↑ ，每秒沿自身z轴向前走0.75个单位
                root.transform.Translate(Vector3.forward * Time.deltaTime * 0.75F);
                if (collision_flag) // 检测碰撞, 若发生碰撞则撤销前进, 改为旋转
                {
                    root.transform.Translate(Vector3.back * Time.deltaTime * 0.75F);
                    root_rotation = root_rotation * Quaternion.Euler(0.0F, 2.5F, 0.0F);
                }
                else
                {
                    time_step = time_step + 1;
                    if (time_step == walk_frame_num)
                    {
                        time_step = 0;
                    }
                }
            }
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {   // 当按下 A 键或 ← ，每秒沿自身y轴逆时针旋转2.5°
                root_rotation = root_rotation * Quaternion.Euler(0.0F, -2.5F, 0.0F);
            }
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {   // 当按下 S 键或 ↓ ，每秒沿自身z轴向后退0.75个单位
                root.transform.Translate(Vector3.back * Time.deltaTime * 0.75F);
                if (collision_flag)
                {
                    root.transform.Translate(Vector3.forward * Time.deltaTime * 0.75F);
                    root_rotation = root_rotation * Quaternion.Euler(0.0F, -2.5F, 0.0F);
                }
                else
                {
                    time_step = time_step - 1;
                    if (time_step == 0)
                    {
                        time_step = walk_frame_num - 1;
                    }
                }
            }
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {   // 当按下 D 键或 → ，每秒沿自身y轴顺时针旋转2.5°
                root_rotation = root_rotation * Quaternion.Euler(0.0F, 2.5F, 0.0F);
            }
            rotationRow = walk_rotations[time_step];
        }

        for (int i = 0; i < joints.Count; i++)
        {
            // ! 进行前向运动学的计算，根据之前解析出的每一帧局部位置、旋转获得每个关节的全局位置、旋转
            if (i == 0)
            {   // root结点没有父结点
                joint_orientation = root_rotation * rotationRow[i];
            }
            else
            {          // 其余各结点旋转等于父结点全局旋转乘自身局部旋转
                joint_orientation = gameObjects[fatherNodes[i]].transform.rotation * rotationRow[i];
            }

            // ! 在以上空白写下你的代码

            // 更新每个关节的全局旋转
            gameObjects[i].transform.rotation = joint_orientation;
        }

        // 按下空格 (Space), 切换自动/手动模式
        if (Input.GetKey(KeyCode.Space))
        {
            if (autoPlayInterval == 0)
            {
                autoPlayInterval = 10;
                if (autoPlayFlag)
                {
                    auto_time_step = time_step;
                    auto_root_rotation = root_rotation;
                    time_step = 0;
                    root.transform.position = auto_root_position;
                    root_rotation = Quaternion.identity;
                }
                else
                {
                    time_step = auto_time_step;
                    root_rotation = auto_root_rotation;
                }
                autoPlayFlag = !autoPlayFlag;
            }
        }
    }

    // 碰撞开始
    void OnCollisionEnter(Collision collision)
    {
        // Debug.Log("Collision Enter!");
        collision_flag = true;
    }

    // 碰撞持续中
    void OnCollisionStay(Collision collision)
    {
        // Debug.Log("Collision Stay!");
        // collision_flag = true;
    }

    // 碰撞结束
    void OnCollisionExit(Collision collision)
    {
        // Debug.Log("Collision Exit!");
        collision_flag = false;
    }
}
