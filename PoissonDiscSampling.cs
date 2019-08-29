using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoissonDiscSampling
{
    static class Extension
    {
        public static float Range(this Random random, float max)
        {
            return random.Range(0f, max);
        }

        public static float Range(this Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        public static int Range(this Random random, int max)
        {
            return random.Range(0, max);
        }

        public static int Range(this Random random, int min, int max)
        {
            return (int)Math.Floor(min + (float)random.NextDouble() * (max - min));
        }

        public static Vector2 insideUnitCircle(this Random random)
        {
            var angle = (float)random.NextDouble() * 2f * Math.PI;
            var r = (float)random.NextDouble();
            return new Vector2(
                r * (float)Math.Cos(angle),
                r * (float)Math.Sin(angle));
        }
    }

    public struct Vector2
    {
        public float x;
        public float y;

        public Vector2(float x, float y) { this.x = x; this.y = y; }

        public void Normalize()
        {
            var mag = magnitude;
            if (mag > kEpsilon) {
                this = this / mag;
            }
            else {
                this = zero;
            }
        }

        public Vector2 normalized
        {
            get {
                var v = new Vector2(x, y);
                v.Normalize();
                return v;
            }
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", x, y);
        }

        public override int GetHashCode()
        {
            return x.GetHashCode() ^ (y.GetHashCode() << 2);
        }

        public override bool Equals(object other)
        {
            if (!(other is Vector2)) {
                return false;
            }

            return Equals((Vector2)other);
        }

        public bool Equals(Vector2 other)
        {
            return x == other.x && y == other.y;
        }

        public float magnitude
        {
            get {
                return (float)Math.Sqrt(x * x + y * y);
            }
        }

        public float sqrMagnitude
        {
            get {
                return x * x + y * y;
            }
        }

        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, Vector2 b) { return new Vector2(a.x * b.x, a.y * b.y); }
        public static Vector2 operator /(Vector2 a, Vector2 b) { return new Vector2(a.x / b.x, a.y / b.y); }
        public static Vector2 operator -(Vector2 a) { return new Vector2(-a.x, -a.y); }
        public static Vector2 operator *(Vector2 a, float d) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator *(float d, Vector2 a) { return new Vector2(a.x * d, a.y * d); }
        public static Vector2 operator /(Vector2 a, float d) { return new Vector2(a.x / d, a.y / d); }
        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            // Returns false in the presence of NaN values.
            var diff_x = lhs.x - rhs.x;
            var diff_y = lhs.y - rhs.y;
            return (diff_x * diff_x + diff_y * diff_y) < kEpsilon * kEpsilon;
        }

        public static bool operator !=(Vector2 lhs, Vector2 rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        static readonly Vector2 zeroVector = new Vector2(0F, 0F);
        static readonly Vector2 oneVector = new Vector2(1F, 1F);
        public static Vector2 zero { get { return zeroVector; } }
        public static Vector2 one { get { return oneVector; } }

        public const float kEpsilon = 0.00001F;
    }

    /// <summary>
    /// @see https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
    /// </summary>
    public static class Algorithm
    {
        public static List<Vector2> Sample2D(float width, float height, float r, int k = 30)
        {
            return Sample2D((int)DateTime.Now.Ticks, width, height, r, k);
        }

        public static List<Vector2> Sample2D(int seed, float width, float height, float r, int k = 30)
        {
            // STEP 0

            // 维度，平面就是2维
            var n = 2;

            // 计算出合理的cell大小
            // cell是一个正方形，为了保证每个cell内部不可能出现多个点，那么cell内的任意点最远距离不能大于r
            // 因为cell内最长的距离是对角线，假设对角线长度是r，那边长就是下面的cell_size
            var cell_size = r / Math.Sqrt(n);

            // 计算出有多少行列的cell
            var cols = (int)Math.Ceiling(width / cell_size);
            var rows = (int)Math.Ceiling(height / cell_size);

            // cells记录了所有合法的点
            var cells = new List<Vector2>();

            // grids记录了每个cell内的点在cells里的索引，-1表示没有点
            var grids = new int[rows, cols];
            for (var i = 0; i < rows; ++i) {
                for (var j = 0; j < cols; ++j) {
                    grids[i, j] = -1;
                }
            }

            // STEP 1
            var random = new Random(seed);

            // 随机选一个起始点
            var x0 = new Vector2(random.Range(width), random.Range(height));
            var col = (int)Math.Floor(x0.x / cell_size);
            var row = (int)Math.Floor(x0.y / cell_size);

            var x0_idx = cells.Count;
            cells.Add(x0);
            grids[row, col] = x0_idx;

            var active_list = new List<int>();
            active_list.Add(x0_idx);

            // STEP 2
            while (active_list.Count > 0) {
                // 随机选一个待处理的点xi
                var xi_idx = active_list[random.Range(active_list.Count)]; // 区间是[0,1)，不用担心溢出。
                var xi = cells[xi_idx];
                var found = false;

                // 以xi为中点，随机找与xi距离在[r,2r)的点xk，并判断该点的合法性
                // 重复k次，如果都找不到，则把xi从active_list中去掉，认为xi附近已经没有合法点了
                for (var i = 0; i < k; ++i) {
                    var dir = random.insideUnitCircle();
                    var xk = xi + (dir.normalized * r + dir * r); // [r,2r)
                    if (xk.x < 0 || xk.x >= width || xk.y < 0 || xk.y >= height) {
                        continue;
                    }

                    col = (int)Math.Floor(xk.x / cell_size);
                    row = (int)Math.Floor(xk.y / cell_size);

                    if (grids[row, col] != -1) {
                        continue;
                    }

                    // 要判断xk的合法性，就是要判断有附近没有点与xk的距离小于r
                    // 由于cell的边长小于r，所以只测试xk所在的cell的九宫格不是够的（考虑xk正好处于cell的边缘的情况）
                    // 正确做法是以xk为中心，做一个边长为2r的正方形，测试这个正方形覆盖到所有cell
                    var ok = true;
                    var min_r = (int)Math.Floor((xk.y - r) / cell_size);
                    var max_r = (int)Math.Floor((xk.y + r) / cell_size);
                    var min_c = (int)Math.Floor((xk.x - r) / cell_size);
                    var max_c = (int)Math.Floor((xk.x + r) / cell_size);
                    for (var or = min_r; or <= max_r; ++or) {
                        if (or < 0 || or >= rows) {
                            continue;
                        }

                        for (var oc = min_c; oc <= max_c; ++oc) {
                            if (oc < 0 || oc >= cols) {
                                continue;
                            }

                            var xj_idx = grids[or, oc];
                            if (xj_idx != -1) {
                                var xj = cells[xj_idx];
                                var dist = (xj - xk).magnitude;
                                if (dist < r) {
                                    ok = false;
                                    goto end_of_distance_check;
                                }
                            }
                        }
                    }

                    end_of_distance_check:
                    if (ok) {
                        var xk_idx = cells.Count;
                        cells.Add(xk);

                        grids[row, col] = xk_idx;
                        active_list.Add(xk_idx);

                        found = true;
                        break;
                    }
                }

                if (!found) {
                    active_list.Remove(xi_idx);
                }
            }

            return cells;
        }
    }
}
